#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs Git hooks for MTG Collection Tracker development

.DESCRIPTION
    This script sets up the pre-commit hook that validates code before
    allowing commits. The hook performs Release builds and solution
    consistency checks to catch issues early.

    Run this once after cloning the repository to enable automatic
    pre-commit validation.

.EXAMPLE
    .\scripts\setup-hooks.ps1

.NOTES
    The hook can be bypassed when needed with: git commit --no-verify
#>

$ErrorActionPreference = 'Stop'

# Colors for output
$Green = [System.ConsoleColor]::Green
$Cyan = [System.ConsoleColor]::Cyan
$Yellow = [System.ConsoleColor]::Yellow

Write-Host "`n🔧 Setting up Git hooks..." -ForegroundColor $Cyan

# Get repository root
$RepoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to find Git repository root" -ForegroundColor Red
    exit 1
}

$HooksDir = Join-Path $RepoRoot ".git\hooks"
$SourceHook = Join-Path $RepoRoot "scripts\pre-commit.ps1"
$DestHook = Join-Path $HooksDir "pre-commit"

# Verify source hook exists
if (-not (Test-Path $SourceHook)) {
    Write-Host "❌ Source hook not found: $SourceHook" -ForegroundColor Red
    exit 1
}

# Verify .git/hooks directory exists
if (-not (Test-Path $HooksDir)) {
    Write-Host "❌ Git hooks directory not found: $HooksDir" -ForegroundColor Red
    Write-Host "   Are you in a Git repository?" -ForegroundColor Yellow
    exit 1
}

# Copy the pre-commit hook
Write-Host "   Installing pre-commit hook..." -ForegroundColor Cyan

# Git hooks need to be shell scripts (Git Bash on Windows, native shell on Unix)
# Create a shell script wrapper that calls PowerShell
$ShellWrapper = @"
#!/bin/sh
# Git pre-commit hook wrapper
# Calls PowerShell script for actual validation

# Get the repository root (go up from .git/hooks/)
REPO_ROOT="`$(cd "`$(dirname "`$0")"/../.. && pwd)"

# Run the PowerShell validation script
pwsh -NoProfile -ExecutionPolicy Bypass -File "`$REPO_ROOT/scripts/pre-commit.ps1"
exit `$?
"@

Set-Content -Path $DestHook -Value $ShellWrapper -Encoding ASCII -NoNewline
Write-Host "   Created Git Bash compatible hook" -ForegroundColor Cyan

# On Unix systems, make the hook executable
if ($IsLinux -or $IsMacOS) {
    chmod +x $DestHook
    Write-Host "   Set execute permissions" -ForegroundColor Cyan
}

Write-Host "`n✅ Git hooks installed successfully!" -ForegroundColor $Green
Write-Host "`nThe pre-commit hook will now run automatically before each commit." -ForegroundColor $Cyan
Write-Host "It validates:" -ForegroundColor $Cyan
Write-Host "  • Release build succeeds (catches warnings-as-errors)" -ForegroundColor Cyan
Write-Host "  • All projects are in the main solution file" -ForegroundColor Cyan
Write-Host "`nTo bypass the hook when needed:" -ForegroundColor $Yellow
Write-Host "  git commit --no-verify -m 'your message'" -ForegroundColor $Yellow
Write-Host ""

exit 0
