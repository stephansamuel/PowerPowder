param(
    [string]$Version,
    [ValidateSet("none", "patch", "minor", "major")]
    [string]$Bump = "none",
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$SkipCoverage,
    [switch]$SkipPackage,
    [switch]$CreateGitTag,
    [switch]$PushGitTag,
    [switch]$Publish,
    [string]$NuGetApiKey,
    [string]$Repository = "PSGallery"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solutionPath = Join-Path $repoRoot "PowerPowder.slnx"
$csprojPath = Join-Path $repoRoot "PowerPowder.Snowflake.PowerShell/PowerPowder.Snowflake.PowerShell.csproj"
$manifestPath = Join-Path $repoRoot "PowerPowder.Snowflake.PowerShell/PowerPowder.Snowflake.PowerShell.psd1"
$testProjectPath = Join-Path $repoRoot "PowerPowder.Snowflake.PowerShell.Tests/PowerPowder.Snowflake.PowerShell.Tests.csproj"
$packageScript = Join-Path $PSScriptRoot "Package-Module.ps1"
$publishScript = Join-Path $PSScriptRoot "Publish-Module.ps1"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    Write-Host "==> $Name"
    & $Action
}

function Get-CurrentVersion {
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    [xml]$xml = Get-Content -Path $ProjectPath
    $value = $xml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($value))
    {
        throw "Unable to determine current version from $ProjectPath"
    }

    return $value.Trim()
}

function Resolve-Version {
    param(
        [Parameter(Mandatory = $true)][string]$Current,
        [string]$Specified,
        [ValidateSet("none", "patch", "minor", "major")][string]$BumpMode
    )

    if (-not [string]::IsNullOrWhiteSpace($Specified) -and $BumpMode -ne "none")
    {
        throw "Provide either -Version or -Bump, not both."
    }

    if (-not [string]::IsNullOrWhiteSpace($Specified))
    {
        return $Specified.Trim()
    }

    if ($BumpMode -eq "none")
    {
        return $Current
    }

    $parsed = [Version]::Parse($Current)
    switch ($BumpMode)
    {
        "patch" { return "{0}.{1}.{2}" -f $parsed.Major, $parsed.Minor, ($parsed.Build + 1) }
        "minor" { return "{0}.{1}.{2}" -f $parsed.Major, ($parsed.Minor + 1), 0 }
        "major" { return "{0}.{1}.{2}" -f ($parsed.Major + 1), 0, 0 }
        default { throw "Unsupported bump mode: $BumpMode" }
    }
}

function Set-ModuleVersion {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string]$TargetVersion
    )

    [xml]$xml = Get-Content -Path $ProjectPath
    $propertyGroup = $xml.Project.PropertyGroup | Select-Object -First 1
    if ($null -eq $propertyGroup)
    {
        throw "Unable to locate PropertyGroup in $ProjectPath"
    }

    if ($null -eq $propertyGroup.Version)
    {
        $node = $xml.CreateElement("Version")
        $node.InnerText = $TargetVersion
        [void]$propertyGroup.AppendChild($node)
    }
    else
    {
        $propertyGroup.Version = $TargetVersion
    }

    $xml.Save($ProjectPath)

    $manifest = Get-Content -Path $ManifestPath -Raw
    $updatedManifest = [regex]::Replace(
        $manifest,
        "(?m)^(\s*ModuleVersion\s*=\s*')([^']+)(')",
        "`$1$TargetVersion`$3")

    if ($updatedManifest -eq $manifest)
    {
        throw "Unable to update ModuleVersion in $ManifestPath"
    }

    Set-Content -Path $ManifestPath -Value $updatedManifest -NoNewline
}

function Invoke-GitTag {
    param(
        [Parameter(Mandatory = $true)][string]$Tag,
        [switch]$Push
    )

    $tagExists = (git tag --list $Tag)
    if ($tagExists)
    {
        throw "Git tag '$Tag' already exists."
    }

    git tag -a $Tag -m "Release $Tag"
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to create git tag '$Tag'."
    }

    if ($Push)
    {
        git push origin $Tag
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to push git tag '$Tag'."
        }
    }
}

if (!(Test-Path $solutionPath)) { throw "Solution file not found: $solutionPath" }
if (!(Test-Path $csprojPath)) { throw "Project file not found: $csprojPath" }
if (!(Test-Path $manifestPath)) { throw "Manifest file not found: $manifestPath" }
if (!(Test-Path $testProjectPath)) { throw "Test project file not found: $testProjectPath" }
if (!(Test-Path $packageScript)) { throw "Packaging script not found: $packageScript" }
if ($Publish -and !(Test-Path $publishScript)) { throw "Publish script not found: $publishScript" }

$currentVersion = Get-CurrentVersion -ProjectPath $csprojPath
$targetVersion = Resolve-Version -Current $currentVersion -Specified $Version -BumpMode $Bump

if ($targetVersion -ne $currentVersion)
{
    Invoke-Step -Name "Updating module version to $targetVersion" -Action {
        Set-ModuleVersion -ProjectPath $csprojPath -ManifestPath $manifestPath -TargetVersion $targetVersion
    }
}
else
{
    Write-Host "==> Version remains $targetVersion"
}

Invoke-Step -Name "Restoring solution" -Action {
    dotnet restore $solutionPath
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
}

Invoke-Step -Name "Building solution ($Configuration)" -Action {
    dotnet build $solutionPath -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}

if (-not $SkipTests)
{
    Invoke-Step -Name "Running tests ($Configuration)" -Action {
        dotnet test $solutionPath -c $Configuration --no-build
        if ($LASTEXITCODE -ne 0) { throw "dotnet test failed." }
    }
}

if (-not $SkipCoverage)
{
    Invoke-Step -Name "Collecting test coverage" -Action {
        dotnet test $testProjectPath -c $Configuration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:CoverletOutput=./TestResults/coverage /p:Include="[PowerPowder.Snowflake.PowerShell]*"
        if ($LASTEXITCODE -ne 0) { throw "Coverage collection failed." }
    }
}

if (-not $SkipPackage)
{
    Invoke-Step -Name "Packaging module" -Action {
        & $packageScript -Configuration $Configuration
    }
}

if ($Publish)
{
    if ([string]::IsNullOrWhiteSpace($NuGetApiKey))
    {
        throw "-NuGetApiKey is required when -Publish is provided."
    }

    Invoke-Step -Name "Publishing module" -Action {
        & $publishScript -NuGetApiKey $NuGetApiKey -Repository $Repository -Configuration $Configuration -SkipPackage
    }
}

if ($CreateGitTag)
{
    $tag = "v$targetVersion"
    Invoke-Step -Name "Creating git tag $tag" -Action {
        Invoke-GitTag -Tag $tag -Push:$PushGitTag
    }
}

Write-Host "Release workflow complete for version $targetVersion"
