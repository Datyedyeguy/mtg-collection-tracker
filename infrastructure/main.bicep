// =============================================================================
// MTG Collection Tracker — Main Infrastructure Entry Point
// =============================================================================
// Deploy with:
//   az deployment group create \
//     --resource-group mtg-tracker-rg \
//     --template-file infrastructure/main.bicep \
//     --parameters infrastructure/parameters/prod.bicepparam
// =============================================================================

targetScope = 'resourceGroup'

// ---------------------------------------------------------------------------
// Parameters
// ---------------------------------------------------------------------------

@description('Short environment name, used as a suffix on all resource names.')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('PostgreSQL administrator login name.')
@minLength(3)
param dbAdminLogin string

@description('PostgreSQL administrator password.')
@secure()
param dbAdminPassword string = ''

@description('JWT signing secret for the API. Must be at least 32 characters.')
@secure()
param jwtSecret string = ''

@description('The URL of the deployed Static Web App frontend (used for CORS).')
param frontendUrl string = ''

@description('Email address to receive budget alert notifications. Leave empty to skip.')
param alertEmail string = ''

// ---------------------------------------------------------------------------
// Variables
// ---------------------------------------------------------------------------

// All resources share a consistent naming suffix: <env>-<short-unique-token>
// The uniqueString() call is deterministic for a given resource group, so
// re-running the deployment always produces the same names.
var suffix = '${environmentName}-${uniqueString(resourceGroup().id)}'

var appServicePlanName = 'asp-mtg-${suffix}'
var apiAppName = 'app-mtg-api-${suffix}'
var staticWebAppName = 'swa-mtg-${suffix}'
var postgresServerName = 'psql-mtg-${suffix}'
var appInsightsName = 'appi-mtg-${suffix}'
var logAnalyticsName = 'log-mtg-${suffix}'

// The API app's CORS origin: use the provided frontend URL if given, otherwise
// allow the Static Web App default hostname (resolved after deployment).
var corsOrigins = !empty(frontendUrl) ? [frontendUrl] : []

// ---------------------------------------------------------------------------
// Modules
// ---------------------------------------------------------------------------

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    appInsightsName: appInsightsName
    logAnalyticsName: logAnalyticsName
    alertEmail: alertEmail
  }
}

module database 'modules/database.bicep' = {
  name: 'database'
  params: {
    location: location
    serverName: postgresServerName
    adminLogin: dbAdminLogin
    adminPassword: dbAdminPassword
  }
}

module webApp 'modules/web-app.bicep' = {
  name: 'webApp'
  params: {
    location: location
    appServicePlanName: appServicePlanName
    apiAppName: apiAppName
    staticWebAppName: staticWebAppName
    // Assemble the connection string here so the secret never appears in a
    // module output (Bicep outputs are visible in deployment history).
    dbConnectionString: 'Host=${database.outputs.serverFqdn};Port=5432;Database=mtgtracker;Username=${dbAdminLogin};Password=${dbAdminPassword};Ssl Mode=Require;Trust Server Certificate=true'
    jwtSecret: jwtSecret
    corsOrigins: corsOrigins
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
// These are referenced by GitHub Actions workflows.

@description('The hostname of the deployed API (e.g. app-mtg-api-prod-abc123.azurewebsites.net).')
output apiHostname string = webApp.outputs.apiHostname

@description('The default hostname of the Static Web App.')
output staticWebAppHostname string = webApp.outputs.staticWebAppHostname

@description('The deployment token for Static Web Apps CI/CD.')
@secure()
output staticWebAppDeploymentToken string = webApp.outputs.staticWebAppDeploymentToken

@description('The PostgreSQL server FQDN.')
output dbServerFqdn string = database.outputs.serverFqdn
