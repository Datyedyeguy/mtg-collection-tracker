#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that no .csproj files override properties managed by Directory.Build.props.

.DESCRIPTION
    Scans all .csproj files in the repository for properties that should only be set
    in Directory.Build.props (at the repo root). This prevents drift where individual
    projects silently override shared build settings.

    The list of managed properties is parsed dynamically from Directory.Build.props,
    so adding a new property there automatically includes it in validation.

    Exit code 0 = all clear, 1 = violations found.

.EXAMPLE
    # Run manually
    ./scripts/Validate-DirectoryBuildProps.ps1

    # Run as part of CI
    pwsh -File scripts/Validate-DirectoryBuildProps.ps1
#>

$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel 2>$null) ?? (Split-Path $PSScriptRoot -Parent)

# Dynamically extract property names from Directory.Build.props
$propsFile = Join-Path $repoRoot 'Directory.Build.props'
if (-not (Test-Path $propsFile)) {
    Write-Host "❌ Directory.Build.props not found at: $propsFile" -ForegroundColor Red
    exit 1
}

[xml]$propsXml = Get-Content $propsFile -Raw
$managedProperties = $propsXml.Project.PropertyGroup.ChildNodes |
Where-Object { $_.NodeType -eq 'Element' } |
ForEach-Object { $_.Name } |
Select-Object -Unique

if ($managedProperties.Count -eq 0) {
    Write-Host "⚠️  No properties found in Directory.Build.props PropertyGroup." -ForegroundColor Yellow
    exit 0
}

Write-Host "Checking for overrides of: $($managedProperties -join ', ')" -ForegroundColor DarkGray

$violations = @()

Get-ChildItem -Path $repoRoot -Filter '*.csproj' -Recurse | ForEach-Object {
    $file = $_
    $relativePath = $file.FullName.Substring($repoRoot.Length + 1)
    $content = Get-Content $file.FullName -Raw

    foreach ($prop in $managedProperties) {
        if ($content -match "<$prop[ >]") {
            $violations += [PSCustomObject]@{
                File     = $relativePath
                Property = $prop
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "`n❌ Directory.Build.props override violations found:" -ForegroundColor Red
    Write-Host ""
    $violations | ForEach-Object {
        Write-Host "  $($_.File): overrides <$($_.Property)>" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "These properties are managed centrally in Directory.Build.props." -ForegroundColor Cyan
    Write-Host "Remove the overrides from the .csproj files listed above." -ForegroundColor Cyan
    Write-Host ""
    exit 1
}

Write-Host "✅ No Directory.Build.props overrides found in .csproj files." -ForegroundColor Green
exit 0
