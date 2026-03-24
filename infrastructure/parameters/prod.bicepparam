// Production environment parameters
// Deployed via: infrastructure-ci.yml on manual approval
//
// Sensitive values (dbAdminPassword, jwtSecret) are sourced from GitHub
// Actions secrets at deploy time — they are never stored in this file.
// See infrastructure-ci.yml for how secrets are passed as --parameters overrides.

using '../main.bicep'

param environmentName = 'prod'
param location = 'eastus2'
param dbAdminLogin = 'mtgadmin'

// NOTE: alertEmail, dbAdminPassword and jwtSecret are NOT set here.
// They are injected by the GitHub Actions workflow from repository secrets:
//   --parameters dbAdminPassword=${{ secrets.DB_ADMIN_PASSWORD }} jwtSecret=${{ secrets.JWT_SECRET }} alertEmail=${{ secrets.ALERT_EMAIL }}
