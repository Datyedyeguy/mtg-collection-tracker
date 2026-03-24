// =============================================================================
// Module: PostgreSQL Flexible Server
// =============================================================================
// Provisions an Azure Database for PostgreSQL — Flexible Server.
// SKU: Burstable B1ms (~$12/month) — 1 vCore, 2 GB RAM.
// Suitable for dev/learning workloads; scale up for staging/prod traffic.
// =============================================================================

param location string
param serverName string
param adminLogin string

@secure()
param adminPassword string

// PostgreSQL version — keep in sync with what's used locally (docker-compose)
var postgresVersion = '17'

// Database name (matches local development database name)
var databaseName = 'mtgtracker'

// ---------------------------------------------------------------------------
// PostgreSQL Flexible Server
// ---------------------------------------------------------------------------

resource pgServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  sku: {
    name: 'Standard_B1ms' // Burstable 1 vCore — cheapest option, ~$12/month
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    version: postgresVersion
    storage: {
      storageSizeGB: 32 // Minimum size for Flexible Server
    }
    backup: {
      backupRetentionDays: 7 // 7-day point-in-time restore window
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled' // Not needed for a learning project
    }
    // Public access with firewall rules — acceptable for learning project
    // For production at scale, switch to VNet injection
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: pgServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// ---------------------------------------------------------------------------
// Firewall rules
// ---------------------------------------------------------------------------

// Allow Azure services (App Service) to connect.
// 0.0.0.0 → 0.0.0.0 is the Azure-internal "Allow Azure services" special rule.
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: pgServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

// Only the FQDN is output — the password is never placed in a Bicep output
// (would be readable in deployment history). The connection string is
// assembled in main.bicep from the FQDN + the secure params already in scope.
output serverFqdn string = pgServer.properties.fullyQualifiedDomainName
