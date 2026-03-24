# ADR-023: OIDC Federated Credentials for GitHub Actions

**Date**: March 24, 2026
**Status**: Accepted

## Context

GitHub Actions workflows need to authenticate with Azure to deploy resources (App Service, Static Web Apps, Bicep templates). Options:

1. **Service Principal + client secret** — create a SP, generate a secret, store in GitHub Secrets
2. **OIDC federated credentials** — GitHub Actions proves its identity via a short-lived JWT; no stored secret
3. **Azure deployment token** — resource-specific tokens (limited to Static Web Apps)

## Decision

Use **OIDC federated credentials** (Workload Identity Federation) for all GitHub Actions → Azure authentication.

Two federated credentials are created per App Registration:

- `github-main` — allows workflows triggered by pushes to `main`
- `github-pull-request` — allows workflows triggered by pull requests (for What-If previews)

## Consequences

**Pros**:

- **No stored secrets**: No long-lived Azure credentials exist anywhere in GitHub Secrets
- **Automatic expiry**: OIDC tokens are valid for a single workflow job (~5 minutes)
- **Audit trail**: Every Azure action is attributed to a specific GitHub workflow run and commit SHA
- **No rotation burden**: No secrets to rotate when they expire
- **Principle of least privilege**: The App Registration has `Contributor` scope on only the target resource group
- **Standard approach**: Recommended by both GitHub and Microsoft for production workloads

**Cons**:

- Slightly more complex initial setup (App Registration + federated credential objects)
- The bootstrap script (`scripts/bootstrap-azure.ps1`) must be run once per environment
- Three GitHub Secrets must still be set manually: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (these are non-sensitive identifiers, not secrets — but GitHub Secrets is the correct place for CI configuration values)

**Why not service principal + client secret?**

- Client secrets expire (90 days by default in Entra ID) and require manual rotation
- Stored secrets are a security liability — if GitHub is compromised, Azure access is too
- OIDC is free and has no expiry management overhead

**Setup**:

All one-time setup is automated by `scripts/bootstrap-azure.ps1`. The script requires:

- `az login` (Azure CLI authenticated)
- `gh auth login` (GitHub CLI authenticated)

The script is **idempotent** — re-running it is safe if anything needs to be recreated.

**Workflow Usage**:

```yaml
permissions:
  id-token: write   # Required for OIDC token request
  contents: read

- uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```
