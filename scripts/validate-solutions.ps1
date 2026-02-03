#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that all .csproj files are referenced in the main solution file.

.DESCRIPTION
    This script scans the repository for .csproj files and ensures they are
    all referenced in MTGCollectionTracker.slnx (the main solution). This
    prevents the scenario where tests are added to partial solutions
    (Backend.slnx, Frontend.slnx) but forgotten in the main solution,
    causing false test coverage confidence when running 'dotnet test'.

    The script performs two checks:
    1. PRIMARY: All projects must be in MTGCollectionTracker.slnx
    2. SAFETY: No projects should be completely orphaned (not in ANY solution)

.EXAMPLE
    .\scripts\validate-solutions.ps1

.NOTES
    Exit Code 0 = All projects are in main solution
    Exit Code 1 = Missing or orphaned projects found (fails CI/CD)
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Colors for output
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Yellow = [System.ConsoleColor]::Yellow
$Cyan = [System.ConsoleColor]::Cyan

Write-Host "`n🔍 Validating solution file consistency..." -ForegroundColor $Cyan

# Get repository root (script is in scripts/ directory)
$RepoRoot = Split-Path -Parent $PSScriptRoot

# Define the main solution file that must contain ALL projects
$MainSolution = "MTGCollectionTracker.slnx"
$MainSolutionPath = Join-Path $RepoRoot $MainSolution

# Verify main solution exists
if (-not (Test-Path $MainSolutionPath)) {
    Write-Host "❌ Main solution file not found: $MainSolution" -ForegroundColor $Red
    exit 1
}

# Find all .csproj files (excluding bin/obj directories)
Write-Host "   Scanning for .csproj files..." -ForegroundColor $Cyan
$AllProjects = Get-ChildItem -Path $RepoRoot -Filter "*.csproj" -Recurse |
Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
ForEach-Object {
    # Return relative path from repo root for easier comparison
    $_.FullName.Replace($RepoRoot, '').TrimStart('\', '/').Replace('\', '/')
}

Write-Host "   Found $($AllProjects.Count) project(s)" -ForegroundColor $Cyan

# Parse the MAIN solution file to extract project references
Write-Host "   Checking main solution: $MainSolution..." -ForegroundColor $Cyan

[xml]$MainSolutionXml = Get-Content -Path $MainSolutionPath -Raw

# Extract all Project/@Path attributes from main solution
# .slnx files are XML format with <Project Path="..." /> elements
$ProjectsInMainSolution = $MainSolutionXml.SelectNodes("//Project/@Path") | ForEach-Object {
    # Normalize path separators to forward slashes for comparison
    $_.Value.Replace('\', '/')
}

Write-Host "   Main solution contains $($ProjectsInMainSolution.Count) project reference(s)`n" -ForegroundColor $Cyan

# Compare: find projects NOT in the main solution
$MissingFromMain = $AllProjects | Where-Object { $_ -notin $ProjectsInMainSolution }

# Report results
$ExitCode = 0

if ($MissingFromMain.Count -eq 0) {
    Write-Host "✅ All projects are referenced in $MainSolution!" -ForegroundColor $Green
}
else {
    Write-Host "❌ Found $($MissingFromMain.Count) project(s) NOT in $MainSolution" -ForegroundColor $Red
    Write-Host "   Impact: 'dotnet test $MainSolution' will skip these projects!`n" -ForegroundColor $Yellow

    foreach ($Project in $MissingFromMain) {
        Write-Host "   - $Project" -ForegroundColor $Yellow
    }
    Write-Host "`n💡 Add these projects to $MainSolution to ensure they are built and tested`n" -ForegroundColor $Yellow
    $ExitCode = 1
}

# Optional: Also check that no projects are orphaned (not in ANY solution)
# This is a safety check for projects that might not even be in the partial solutions
Write-Host "   Checking for completely orphaned projects..." -ForegroundColor $Cyan

$AllSolutionFiles = Get-ChildItem -Path $RepoRoot -Filter "*.slnx" -Recurse |
Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

$ProjectsInAnySolution = @()
foreach ($SolutionFile in $AllSolutionFiles) {
    [xml]$SolutionXml = Get-Content -Path $SolutionFile.FullName -Raw
    $Projects = $SolutionXml.SelectNodes("//Project/@Path") | ForEach-Object {
        $_.Value.Replace('\', '/')
    }
    $ProjectsInAnySolution += $Projects
}

$ProjectsInAnySolution = $ProjectsInAnySolution | Select-Object -Unique
$CompletelyOrphaned = $AllProjects | Where-Object { $_ -notin $ProjectsInAnySolution }

if ($CompletelyOrphaned.Count -eq 0) {
    Write-Host "   No orphaned projects found`n" -ForegroundColor $Cyan
}
else {
    Write-Host "`n⚠️  Found $($CompletelyOrphaned.Count) project(s) not in ANY solution file:" -ForegroundColor $Yellow
    foreach ($Project in $CompletelyOrphaned) {
        Write-Host "   - $Project" -ForegroundColor $Yellow
    }
    Write-Host "`n💡 These projects are completely disconnected from the build`n" -ForegroundColor $Yellow
    $ExitCode = 1
}

exit $ExitCode
