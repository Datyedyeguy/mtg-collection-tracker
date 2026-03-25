// =============================================================================
// Module: App Service (API) + Static Web App (Frontend)
// =============================================================================
// App Service Linux B1: ~$13/month (1 vCore, 1.75 GB RAM)
// Static Web Apps: Free tier (includes preview environments per PR)
// =============================================================================

param location string
param appServicePlanName string
param apiAppName string
param staticWebAppName string

@secure()
param dbConnectionString string

@secure()
param jwtSecret string

param corsOrigins string[]
param appInsightsConnectionString string

// ---------------------------------------------------------------------------
// App Service Plan (Linux B1)
// ---------------------------------------------------------------------------

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

// ---------------------------------------------------------------------------
// App Service — ASP.NET Core 10 API
// ---------------------------------------------------------------------------

resource apiApp 'Microsoft.Web/sites@2024-04-01' = {
  name: apiAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      ftpsState: 'Disabled' // No FTP — deploy only via GitHub Actions
      http20Enabled: true
      minTlsVersion: '1.2'
      // Health check — App Service restarts the instance if /api/health fails 10+ times
      healthCheckPath: '/api/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        // JwtSettings — matches the JwtSettings section in appsettings.json
        {
          name: 'JwtSettings__Secret'
          value: jwtSecret
        }
        {
          name: 'JwtSettings__Issuer'
          value: 'https://${apiAppName}.azurewebsites.net'
        }
        {
          name: 'JwtSettings__Audience'
          // Populated after Static Web App is deployed; overridden by infrastructure-ci workflow
          value: 'https://${staticWebAppName}.azurestaticapps.net'
        }
        {
          name: 'JwtSettings__AccessTokenExpiryMinutes'
          value: '15'
        }
        {
          name: 'JwtSettings__RefreshTokenExpiryDays'
          value: '7'
        }
        // CORS — only the frontend origin is allowed
        {
          name: 'Cors__AllowedOrigins__0'
          value: !empty(corsOrigins) ? corsOrigins[0] : 'https://${staticWebAppName}.azurestaticapps.net'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: dbConnectionString
          type: 'Custom'
        }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Static Web App — Blazor WebAssembly Frontend
// ---------------------------------------------------------------------------
// Free tier includes:
//   - Custom domains + free TLS
//   - Automatic PR preview environments
//   - Global CDN distribution

resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: staticWebAppName
  location: location // SWA is available in limited regions; eastus2 is supported
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // GitHub integration is wired via the deployment token in the CI workflow,
    // not through the Bicep 'repositoryUrl' property, so we leave it empty here.
    // This avoids Bicep trying to manage the GitHub connection directly.
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output apiHostname string = apiApp.properties.defaultHostName

output staticWebAppHostname string = staticWebApp.properties.defaultHostname
