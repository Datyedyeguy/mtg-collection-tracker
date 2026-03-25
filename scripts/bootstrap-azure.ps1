#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-time Azure bootstrap for MTG Collection Tracker.

.DESCRIPTION
    Creates all Azure resources and GitHub configuration needed for CI/CD.
    Run this ONCE after cloning, while authenticated to both Azure CLI and
    GitHub CLI. Everything it creates is idempotent — safe to re-run.

    What it does:
      1. Creates the Azure resource group
      2. Creates an Entra ID App Registration with federated OIDC credentials
         (no passwords or certificates — GitHub proves identity via signed JWT)
      3. Grants the App Registration Contributor access to the resource group
      4. Stores the three non-sensitive IDs as GitHub Actions secrets so
         workflows can authenticate without any stored passwords

    Prerequisites:
      - az CLI installed and logged in:   az login
      - gh CLI installed and logged in:   gh auth login
      - Git remote 'origin' pointing to GitHub

.PARAMETER ResourceGroup
    Azure resource group name. Default: mtg-tracker-rg

.PARAMETER Location
    Azure region. Default: eastus2

.PARAMETER AppName
    Entra App Registration display name. Default: mtg-collection-tracker-github

.EXAMPLE
    .\scripts\bootstrap-azure.ps1

.EXAMPLE
    .\scripts\bootstrap-azure.ps1 -Location westus2 -ResourceGroup my-rg
#>

param(
    [string] $ResourceGroup = 'mtg-tracker-rg',
    [string] $Location = 'eastus2',
    [string] $AppName = 'mtg-collection-tracker-github'
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Step([string] $msg) {
    Write-Host "`n▶  $msg" -ForegroundColor Cyan
}

function Write-Ok([string] $msg) {
    Write-Host "   ✅ $msg" -ForegroundColor Green
}

function Write-Info([string] $msg) {
    Write-Host "   ℹ  $msg" -ForegroundColor Gray
}

function Require-Command([string] $cmd, [string] $hint) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Host "❌ '$cmd' not found. $hint" -ForegroundColor Red
        exit 1
    }
}

# ---------------------------------------------------------------------------
# Prerequisites check
# ---------------------------------------------------------------------------

Write-Host "`n=================================================" -ForegroundColor Yellow
Write-Host "  MTG Collection Tracker — Azure Bootstrap"        -ForegroundColor Yellow
Write-Host "================================================="  -ForegroundColor Yellow

Require-Command 'az' 'Install Azure CLI: https://docs.microsoft.com/cli/azure/install-azure-cli'
Require-Command 'gh' 'Install GitHub CLI: https://cli.github.com/'

# Verify az is logged in
Write-Step "Checking Azure login..."
$accountJson = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not logged in to Azure. Run: az login" -ForegroundColor Red
    exit 1
}
$account = $accountJson | ConvertFrom-Json
$subscriptionId = $account.id
$tenantId = $account.tenantId
Write-Ok "Logged in as: $($account.user.name)"
Write-Info "Subscription: $($account.name) ($subscriptionId)"

# Verify gh is logged in
Write-Step "Checking GitHub login..."
$null = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not logged in to GitHub CLI. Run: gh auth login" -ForegroundColor Red
    exit 1
}

# Resolve GitHub repo from git remote
$gitRemote = git remote get-url origin 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ No git remote 'origin' found." -ForegroundColor Red
    exit 1
}
# Parse owner/repo from https://github.com/owner/repo.git or git@github.com:owner/repo.git
if ($gitRemote -match 'github\.com[:/](.+?)(?:\.git)?$') {
    $githubRepo = $Matches[1]   # e.g. "Datyedyeguy/mtg-collection-tracker"
}
else {
    Write-Host "❌ Could not parse GitHub repo from remote: $gitRemote" -ForegroundColor Red
    exit 1
}
Write-Ok "GitHub repo: $githubRepo"

# ---------------------------------------------------------------------------
# Step 1: Resource group
# ---------------------------------------------------------------------------

Write-Step "Creating resource group '$ResourceGroup' in '$Location'..."
$rgExists = az group exists --name $ResourceGroup | ConvertFrom-Json
if ($rgExists) {
    Write-Info "Resource group already exists — skipping creation."
}
else {
    az group create --name $ResourceGroup --location $Location | Out-Null
    Write-Ok "Resource group created."
}
$resourceGroupId = (az group show --name $ResourceGroup --query id -o tsv)

# ---------------------------------------------------------------------------
# Step 2: App Registration (Entra ID)
# ---------------------------------------------------------------------------

Write-Step "Checking for App Registration '$AppName'..."
$existingApp = az ad app list --display-name $AppName --query "[0]" -o json | ConvertFrom-Json
if ($existingApp) {
    $clientId = $existingApp.appId
    Write-Info "App Registration already exists (clientId: $clientId) — reusing."
}
else {
    Write-Info "Creating App Registration..."
    $newApp = az ad app create --display-name $AppName --query '{appId:appId, id:id}' -o json | ConvertFrom-Json
    $clientId = $newApp.appId
    Write-Ok "App Registration created (clientId: $clientId)."
}

# Ensure a Service Principal exists for the App Registration
$spExists = az ad sp show --id $clientId 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Info "Creating Service Principal..."
    az ad sp create --id $clientId | Out-Null
    Write-Ok "Service Principal created."
}
else {
    Write-Info "Service Principal already exists — skipping."
}

# ---------------------------------------------------------------------------
# Step 3: Federated OIDC credentials
# ---------------------------------------------------------------------------

# We create two federated credentials:
#   1. main branch — used by backend-ci, frontend-ci, and infrastructure-ci deploy jobs
#   2. pull_request — used by PR validation jobs that need Azure for what-if previews
#   3. environment:production — used by deploy jobs that specify 'environment: production'
#      (GitHub changes the OIDC subject claim when a job targets a named environment)

$federatedCredentials = @(
    @{
        name    = 'github-main'
        subject = "repo:${githubRepo}:ref:refs/heads/main"
        desc    = 'GitHub Actions — main branch deploys'
    },
    @{
        name    = 'github-pull-request'
        subject = "repo:${githubRepo}:pull_request"
        desc    = 'GitHub Actions — pull request checks'
    },
    @{
        name    = 'github-environment-production'
        subject = "repo:${githubRepo}:environment:production"
        desc    = 'GitHub Actions — production environment deployments'
    }
)

foreach ($cred in $federatedCredentials) {
    Write-Step "Configuring federated credential '$($cred.name)'..."

    # Check if it already exists
    $existing = az ad app federated-credential list --id $clientId --query "[?name=='$($cred.name)']" -o json | ConvertFrom-Json
    if ($existing.Count -gt 0) {
        Write-Info "Credential '$($cred.name)' already exists — skipping."
        continue
    }

    # Write JSON to a temp file to avoid PowerShell shell-quoting issues with
    # inline JSON strings passed to az CLI (property names lose double quotes).
    $credJson = @{
        name        = $cred.name
        issuer      = 'https://token.actions.githubusercontent.com'
        subject     = $cred.subject
        description = $cred.desc
        audiences   = @('api://AzureADTokenExchange')
    } | ConvertTo-Json
    $tempFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tempFile -Value $credJson -Encoding UTF8

    az ad app federated-credential create --id $clientId --parameters $tempFile
    if ($LASTEXITCODE -ne 0) {
        Remove-Item $tempFile -Force
        throw "Failed to create federated credential '$($cred.name)'."
    }
    Remove-Item $tempFile -Force
    Write-Ok "Federated credential '$($cred.name)' created."
}

# ---------------------------------------------------------------------------
# Step 4: Role assignment (Contributor on the resource group)
# ---------------------------------------------------------------------------

Write-Step "Assigning Contributor role to Service Principal on resource group..."
$existingRole = az role assignment list `
    --assignee $clientId `
    --role Contributor `
    --scope $resourceGroupId `
    --query "[0]" -o json | ConvertFrom-Json

if ($existingRole) {
    Write-Info "Role assignment already exists — skipping."
}
else {
    az role assignment create `
        --assignee $clientId `
        --role Contributor `
        --scope $resourceGroupId | Out-Null
    Write-Ok "Role assignment created."
}

# ---------------------------------------------------------------------------
# Step 5: GitHub secrets
# ---------------------------------------------------------------------------

Write-Step "Storing IDs as GitHub Actions secrets..."
Write-Info "These are non-sensitive public identifiers — no passwords or keys involved."

$secrets = @{
    AZURE_CLIENT_ID       = $clientId
    AZURE_TENANT_ID       = $tenantId
    AZURE_SUBSCRIPTION_ID = $subscriptionId
    AZURE_RESOURCE_GROUP  = $ResourceGroup
}

foreach ($secret in $secrets.GetEnumerator()) {
    gh secret set $secret.Key --body $secret.Value --repo $githubRepo
    Write-Ok "Secret '$($secret.Key)' set."
}

# ALERT_EMAIL must be set manually — prompt the user
$alertEmail = Read-Host "`nEnter your email address for Azure budget alerts (leave blank to skip)"
if (-not [string]::IsNullOrWhiteSpace($alertEmail)) {
    gh secret set ALERT_EMAIL --body $alertEmail --repo $githubRepo
    Write-Ok "Secret 'ALERT_EMAIL' set."
}
else {
    Write-Info "Skipped ALERT_EMAIL — budget alerts will fire but notify no one. Set manually later."
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host "  ✅ Bootstrap complete!"                            -ForegroundColor Green
Write-Host "================================================="  -ForegroundColor Green
Write-Host ""
Write-Host "  Resource group : $ResourceGroup ($Location)"
Write-Host "  App Registration: $AppName"
Write-Host "  Client ID       : $clientId"
Write-Host "  Tenant ID       : $tenantId"
Write-Host "  Subscription    : $subscriptionId"
Write-Host ""
Write-Host "  GitHub secrets set on: $githubRepo"
Write-Host "    - AZURE_CLIENT_ID"
Write-Host "    - AZURE_TENANT_ID"
Write-Host "    - AZURE_SUBSCRIPTION_ID"
Write-Host "    - AZURE_RESOURCE_GROUP"
Write-Host ""
Write-Host "  Next step: run the infrastructure workflow to provision Azure resources." -ForegroundColor Cyan
Write-Host "  From GitHub: Actions → 'Infrastructure CI' → Run workflow"              -ForegroundColor Cyan
Write-Host ""
