param(
    [Parameter(Mandatory = $true)]
    [string]$NuGetApiKey,
    [string]$Repository = "PSGallery",
    [string]$Configuration = "Release",
    [switch]$SkipPackage,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "PowerPowder.Snowflake.PowerShell/PowerPowder.Snowflake.PowerShell.csproj"
$packageScript = Join-Path $PSScriptRoot "Package-Module.ps1"

if (!(Test-Path $projectPath))
{
    throw "Could not find module project at $projectPath"
}

if (!(Test-Path $packageScript))
{
    throw "Could not find packaging script at $packageScript"
}

if (-not $SkipPackage)
{
    Write-Host "Packaging module before publish..."
    & $packageScript -Configuration $Configuration
}

[xml]$projectXml = Get-Content -Path $projectPath
$version = $projectXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version))
{
    throw "Could not determine module version from csproj."
}

$modulePath = Join-Path $repoRoot "dist/PowerPowder.Snowflake.PowerShell/$version"
if (!(Test-Path $modulePath))
{
    throw "Expected packaged module folder not found: $modulePath"
}

Write-Host "Publishing module version $version from $modulePath to repository $Repository"
if ($WhatIf)
{
    Write-Host "Dry run enabled. No publish will be performed."
    Write-Host "Validated module path: $modulePath"
    Write-Host "Validated repository: $Repository"
    Write-Host "Publish command would be: Publish-Module -Path '$modulePath' -Repository '$Repository' -NuGetApiKey '<redacted>' -Force"
    return
}

 $publishParams = @{
    Path = $modulePath
    Repository = $Repository
    NuGetApiKey = $NuGetApiKey
    Force = $true
}

Publish-Module @publishParams

Write-Host "Publish command completed."
