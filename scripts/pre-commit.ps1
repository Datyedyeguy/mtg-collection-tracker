#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Git pre-commit hook for MTG Collection Tracker

.DESCRIPTION
    This hook runs automatically before 'git commit' completes.
    It performs the following validations:

    1. Release Build Check - Ensures code compiles in Release mode
       (catches warnings that are treated as errors in CI/CD)

    2. Solution Consistency - Verifies all projects are in main solution
       (prevents tests from being skipped)

    3. EF Core Migrations - Checks for pending model changes without migrations

    4. Directory.Build.props Overrides - Ensures .csproj files don't override
       centrally managed build properties

    If any validation fails, the commit is aborted with an error message.

.NOTES
    - This hook can be bypassed with: git commit --no-verify
    - Install this hook by running: .\scripts\setup-hooks.ps1
    - Exit Code 0 = All validations passed, commit proceeds
    - Exit Code 1 = Validation failed, commit aborted

.EXAMPLE
    # Normal commit workflow (hook runs automatically)
    git add .
    git commit -m "feat: add new feature"

    # Bypass hook if needed (use sparingly!)
    git commit --no-verify -m "wip: temporary checkpoint"
#>

$ErrorActionPreference = 'Stop'

# Colors for output
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Cyan = [System.ConsoleColor]::Cyan
$Yellow = [System.ConsoleColor]::Yellow

Write-Host "`n" -NoNewline
Write-Host "=======================================================" -ForegroundColor $Cyan
Write-Host "  MTG Collection Tracker - Pre-Commit Validation" -ForegroundColor $Cyan
Write-Host "=======================================================" -ForegroundColor $Cyan

# Get repository root
$RepoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to find Git repository root" -ForegroundColor $Red
    exit 1
}

$ValidationsPassed = 0
$ValidationsTotal = 4

# ============================================================================
# Validation 1: Release Build Check
# ============================================================================
Write-Host "`n[1/$ValidationsTotal] 🔨 Building in Release configuration..." -ForegroundColor $Cyan

# Run dotnet build with Release configuration
# This catches warnings that are treated as errors (e.g., unused fields)
# We use --no-restore to speed this up since restore happens less frequently
dotnet build "$RepoRoot\MTGCollectionTracker.slnx" --configuration Release --no-restore --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "        ✅ Release build succeeded" -ForegroundColor $Green
    $ValidationsPassed++
}
else {
    Write-Host "`n        ❌ Release build failed!" -ForegroundColor $Red
    Write-Host "        💡 Fix compilation errors before committing" -ForegroundColor $Yellow
    Write-Host "        💡 Or bypass with: git commit --no-verify`n" -ForegroundColor $Yellow
    exit 1
}

# ============================================================================
# Validation 2: Solution Consistency Check
# ============================================================================
Write-Host "`n[2/$ValidationsTotal] 📋 Validating solution consistency..." -ForegroundColor $Cyan

# Run the solution validation script
& "$RepoRoot\scripts\validate-solutions.ps1"

if ($LASTEXITCODE -eq 0) {
    Write-Host "        ✅ Solution validation passed" -ForegroundColor $Green
    $ValidationsPassed++
}
else {
    Write-Host "`n        ❌ Solution validation failed!" -ForegroundColor $Red
    Write-Host "        💡 Add missing projects to MTGCollectionTracker.slnx" -ForegroundColor $Yellow
    Write-Host "        💡 Or bypass with: git commit --no-verify`n" -ForegroundColor $Yellow
    exit 1
}

# ============================================================================
# Validation 3: EF Core Pending Model Changes
# ============================================================================
Write-Host "`n[3/$ValidationsTotal] 🗄️  Checking for pending EF Core migrations..." -ForegroundColor $Cyan

# EF Core 8+ CLI command: exits 0 if snapshot matches model, 1 if not.
# This catches the case where an entity changed but 'dotnet ef migrations add' was not run.
dotnet ef migrations has-pending-model-changes `
    --project "$RepoRoot\src\backend\MTGCollectionTracker.Data" `
    --startup-project "$RepoRoot\src\backend\MTGCollectionTracker.Api" `
    --no-build 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "        ✅ No pending model changes" -ForegroundColor $Green
    $ValidationsPassed++
}
else {
    Write-Host "`n        ❌ EF Core model has changes without a migration!" -ForegroundColor $Red
    Write-Host "        💡 Run: dotnet ef migrations add <Name>" -ForegroundColor $Yellow
    Write-Host "               --project src/backend/MTGCollectionTracker.Data" -ForegroundColor $Yellow
    Write-Host "               --startup-project src/backend/MTGCollectionTracker.Api" -ForegroundColor $Yellow
    Write-Host "        💡 Or bypass with: git commit --no-verify`n" -ForegroundColor $Yellow
    exit 1
}

# ============================================================================
# Validation 4: Directory.Build.props Override Check
# ============================================================================
Write-Host "`n[4/$ValidationsTotal] 📐 Checking for Directory.Build.props overrides..." -ForegroundColor $Cyan

& "$RepoRoot\scripts\Validate-DirectoryBuildProps.ps1"

if ($LASTEXITCODE -eq 0) {
    Write-Host "        ✅ No .csproj property overrides" -ForegroundColor $Green
    $ValidationsPassed++
}
else {
    Write-Host "`n        ❌ Directory.Build.props override violations found!" -ForegroundColor $Red
    Write-Host "        💡 Remove overridden properties from .csproj files" -ForegroundColor $Yellow
    Write-Host "        💡 Or bypass with: git commit --no-verify`n" -ForegroundColor $Yellow
    exit 1
}

# ============================================================================
# Success!
# ============================================================================
Write-Host "`n" -NoNewline
Write-Host "=======================================================" -ForegroundColor $Green
Write-Host "  ✅ All validations passed ($ValidationsPassed/$ValidationsTotal)" -ForegroundColor $Green
Write-Host "  Proceeding with commit..." -ForegroundColor $Green
Write-Host "=======================================================" -ForegroundColor $Green
Write-Host ""

exit 0
