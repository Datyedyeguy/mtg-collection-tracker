// =============================================================================
// Module: Application Insights + Log Analytics Workspace
// =============================================================================
// Provides structured logging, request tracing, and performance monitoring.
// Log Analytics is the backing store for App Insights.
// Estimated cost: ~$0–$5/month (first 5 GB/month free under the Basic tier).
// =============================================================================

param location string
param appInsightsName string
param logAnalyticsName string

@description('Email address to notify when budget thresholds are crossed. Leave empty to skip alerts.')
param alertEmail string = ''

// ---------------------------------------------------------------------------
// Log Analytics Workspace
// ---------------------------------------------------------------------------
// App Insights workspace-based mode requires a Log Analytics workspace.
// Using the PerGB2018 pricing tier (pay-per-GB ingested, cheapest option).

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30 // Minimum retention; free up to 31 days
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// ---------------------------------------------------------------------------
// Application Insights
// ---------------------------------------------------------------------------

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: 30
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ---------------------------------------------------------------------------
// Budget alerts
// ---------------------------------------------------------------------------
// Alert at 50%, 83%, and 100% of a $150/month learning budget.
// Notifications go to an email address set at the subscription level.
// NOTE: Budget scope must be subscription or resource group — these are
// resource-group scoped so they only track costs for this project.

resource budget 'Microsoft.Consumption/budgets@2024-08-01' = {
  name: 'mtg-tracker-monthly-budget'
  properties: {
    category: 'Cost'
    amount: 150
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: '2026-04-01'
    }
    filter: {
      dimensions: {
        name: 'ResourceGroupName'
        operator: 'In'
        values: [resourceGroup().name]
      }
    }
    notifications: {
      alert_75: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 50 // 50% of $150 = $75
        contactEmails: !empty(alertEmail) ? [alertEmail] : []
        thresholdType: 'Actual'
      }
      alert_125: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 83 // 83% of $150 = ~$125
        contactEmails: !empty(alertEmail) ? [alertEmail] : []
        thresholdType: 'Actual'
      }
      alert_150: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100 // 100% = $150
        contactEmails: !empty(alertEmail) ? [alertEmail] : []
        thresholdType: 'Actual'
      }
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
