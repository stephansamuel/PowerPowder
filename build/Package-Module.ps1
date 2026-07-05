param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectRoot = Join-Path $PSScriptRoot "../PowerPowder.Snowflake.PowerShell"
$projectPath = Join-Path $projectRoot "PowerPowder.Snowflake.PowerShell.csproj"

Write-Host "Building module project..."
dotnet build $projectPath -c $Configuration | Out-Host

$tfm = "netstandard2.0"
$buildOutput = Join-Path $projectRoot "bin/$Configuration/$tfm"
$manifestSource = Join-Path $projectRoot "PowerPowder.Snowflake.PowerShell.psd1"

[xml]$projectXml = Get-Content -Path $projectPath
$version = $projectXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version))
{
    throw "Could not determine module version from csproj."
}

$distRoot = Join-Path $PSScriptRoot "../dist/PowerPowder.Snowflake.PowerShell/$version"
if (Test-Path $distRoot)
{
    Remove-Item -Path $distRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null

Write-Host "Copying compiled module artifacts to $distRoot"
Get-ChildItem -Path $buildOutput -File |
    Where-Object { $_.Extension -in ".dll", ".json", ".pdb", ".xml" } |
    ForEach-Object {
        Copy-Item -Path $_.FullName -Destination (Join-Path $distRoot $_.Name)
    }

Copy-Item -Path $manifestSource -Destination (Join-Path $distRoot "PowerPowder.Snowflake.PowerShell.psd1")

$zipPath = Join-Path $PSScriptRoot "../dist/PowerPowder.Snowflake.PowerShell.$version.zip"
if (Test-Path $zipPath)
{
    Remove-Item -Path $zipPath -Force
}

Compress-Archive -Path $distRoot -DestinationPath $zipPath

Write-Host "Module package created: $distRoot"
Write-Host "Zip archive created: $zipPath"
