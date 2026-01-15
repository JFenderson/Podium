// ============================================================================
// Podium Application - Azure Infrastructure as Code (Bicep)
// ============================================================================
// This template provisions all required Azure resources for the Podium application
// including App Services, SQL Database, Storage, CDN, Key Vault, and monitoring.
// ============================================================================

@description('Environment name (e.g., staging, production)')
param environment string

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Application name prefix')
param appName string = 'podium'

@description('Administrator login for SQL Server')
@secure()
param sqlAdministratorLogin string

@description('Administrator password for SQL Server')
@secure()
param sqlAdministratorPassword string

@description('JWT secret for token signing (min 32 characters)')
@secure()
param jwtSecret string

@description('SendGrid API key for email service')
@secure()
param sendGridApiKey string

@description('Tags to apply to all resources')
param tags object = {
  Application: 'Podium'
  Environment: environment
  ManagedBy: 'Bicep'
}

// ============================================================================
// Variables
// ============================================================================

var uniqueSuffix = uniqueString(resourceGroup().id)
var appServicePlanName = '${appName}-asp-${environment}-${uniqueSuffix}'
var backendAppServiceName = '${appName}-backend-${environment}-${uniqueSuffix}'
var frontendAppServiceName = '${appName}-frontend-${environment}-${uniqueSuffix}'
var sqlServerName = '${appName}-sql-${environment}-${uniqueSuffix}'
var sqlDatabaseName = 'PodiumDb'
var storageAccountName = '${appName}storage${environment}${uniqueSuffix}'
var keyVaultName = '${appName}-kv-${environment}-${uniqueSuffix}'
var appInsightsName = '${appName}-insights-${environment}-${uniqueSuffix}'
var logAnalyticsName = '${appName}-logs-${environment}-${uniqueSuffix}'
var cdnProfileName = '${appName}-cdn-${environment}-${uniqueSuffix}'
var cdnEndpointName = '${appName}-cdn-endpoint-${environment}-${uniqueSuffix}'

// ============================================================================
// Log Analytics Workspace (must be created first)
// ============================================================================

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 90
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// ============================================================================
// Application Insights
// ============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: 90
    IngestionMode: 'LogAnalytics'
  }
}

// ============================================================================
// App Service Plan (P1v2 - Production Grade)
// ============================================================================

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'P1v2'
    tier: 'PremiumV2'
    capacity: 1
  }
  properties: {
    reserved: true // Linux
  }
}

// ============================================================================
// Backend App Service (.NET/C#)
// ============================================================================

resource backendAppService 'Microsoft.Web/sites@2022-09-01' = {
  name: backendAppServiceName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'production' ? 'Production' : 'Staging'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'JWT__Secret'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/jwt-secret/)'
        }
        {
          name: 'AzureStorage__ConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/storage-connection-string/)'
        }
        {
          name: 'SendGrid__ApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/sendgrid-api-key/)'
        }
        {
          name: 'AllowedOrigins__0'
          value: 'https://${frontendAppService.properties.defaultHostName}'
        }
        {
          name: 'AllowedOrigins__1'
          value: 'https://${cdnEndpoint.properties.hostName}'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/sql-connection-string/)'
          type: 'SQLAzure'
        }
      ]
    }
  }
}

// Auto-scaling rules for backend
resource backendAutoScale 'Microsoft.Insights/autoscalesettings@2022-10-01' = {
  name: '${backendAppServiceName}-autoscale'
  location: location
  tags: tags
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'Auto scale based on CPU'
        capacity: {
          minimum: '1'
          maximum: '8'
          default: '1'
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 40
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }
        ]
      }
    ]
  }
}

// ============================================================================
// Frontend App Service (TypeScript/HTML/Angular)
// ============================================================================

resource frontendAppService 'Microsoft.Web/sites@2022-09-01' = {
  name: frontendAppServiceName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'NODE|20-lts'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'API_URL'
          value: 'https://${backendAppService.properties.defaultHostName}'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

// ============================================================================
// SQL Server and Database (S2 tier with geo-replication)
// ============================================================================

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Firewall rule to allow Azure services
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'S2'
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456000 // 250 GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Geo'
  }
}

// Long-term retention policy
resource longTermRetention 'Microsoft.Sql/servers/databases/backupLongTermRetentionPolicies@2022-05-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    weeklyRetention: 'P5W'
    monthlyRetention: 'P12M'
    yearlyRetention: 'P7Y'
    weekOfYear: 1
  }
}

// Short-term retention policy (point-in-time restore)
resource shortTermRetention 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2022-05-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    retentionDays: 35
    diffBackupIntervalInHours: 24
  }
}

// ============================================================================
// Storage Account (Blob Storage for videos, thumbnails, assets)
// ============================================================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_GRS' // Geo-redundant storage
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Blob service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: [
            'https://${frontendAppService.properties.defaultHostName}'
            'https://${cdnEndpoint.properties.hostName}'
          ]
          allowedMethods: [
            'GET'
            'POST'
            'PUT'
          ]
          allowedHeaders: [
            '*'
          ]
          exposedHeaders: [
            '*'
          ]
          maxAgeInSeconds: 3600
        }
      ]
    }
    deleteRetentionPolicy: {
      enabled: true
      days: 30
    }
  }
}

// Container for video uploads
resource videosContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'videos'
  properties: {
    publicAccess: 'None'
  }
}

// Container for thumbnails
resource thumbnailsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'thumbnails'
  properties: {
    publicAccess: 'None'
  }
}

// Container for static assets
resource assetsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'assets'
  properties: {
    publicAccess: 'None'
  }
}

// ============================================================================
// Key Vault (Secrets Management)
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enableRbacAuthorization: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: backendAppService.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
      {
        tenantId: subscription().tenantId
        objectId: frontendAppService.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Store secrets in Key Vault
resource jwtSecretKV 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'jwt-secret'
  properties: {
    value: jwtSecret
  }
}

resource sqlConnectionStringKV 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'sql-connection-string'
  properties: {
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
}

resource storageConnectionStringKV 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'storage-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
  }
}

resource sendGridApiKeyKV 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'sendgrid-api-key'
  properties: {
    value: sendGridApiKey
  }
}

// ============================================================================
// CDN Profile and Endpoint
// ============================================================================

resource cdnProfile 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: cdnProfileName
  location: 'global'
  tags: tags
  sku: {
    name: 'Standard_Microsoft'
  }
}

resource cdnEndpoint 'Microsoft.Cdn/profiles/endpoints@2023-05-01' = {
  parent: cdnProfile
  name: cdnEndpointName
  location: 'global'
  tags: tags
  properties: {
    originHostHeader: frontendAppService.properties.defaultHostName
    isHttpAllowed: false
    isHttpsAllowed: true
    queryStringCachingBehavior: 'IgnoreQueryString'
    contentTypesToCompress: [
      'text/plain'
      'text/html'
      'text/css'
      'application/x-javascript'
      'text/javascript'
      'application/javascript'
      'application/json'
      'application/xml'
    ]
    isCompressionEnabled: true
    origins: [
      {
        name: 'frontend-origin'
        properties: {
          hostName: frontendAppService.properties.defaultHostName
          httpPort: 80
          httpsPort: 443
          originHostHeader: frontendAppService.properties.defaultHostName
        }
      }
    ]
    deliveryPolicy: {
      rules: [
        {
          name: 'StaticAssetsCaching'
          order: 1
          conditions: [
            {
              name: 'UrlFileExtension'
              parameters: {
                typeName: 'DeliveryRuleUrlFileExtensionMatchConditionParameters'
                operator: 'Equal'
                matchValues: [
                  'js'
                  'css'
                  'png'
                  'jpg'
                  'jpeg'
                  'gif'
                  'svg'
                  'ico'
                  'woff'
                  'woff2'
                ]
              }
            }
          ]
          actions: [
            {
              name: 'CacheExpiration'
              parameters: {
                typeName: 'DeliveryRuleCacheExpirationActionParameters'
                cacheBehavior: 'Override'
                cacheType: 'All'
                cacheDuration: '7.00:00:00'
              }
            }
          ]
        }
        {
          name: 'HTMLCaching'
          order: 2
          conditions: [
            {
              name: 'UrlFileExtension'
              parameters: {
                typeName: 'DeliveryRuleUrlFileExtensionMatchConditionParameters'
                operator: 'Equal'
                matchValues: [
                  'html'
                ]
              }
            }
          ]
          actions: [
            {
              name: 'CacheExpiration'
              parameters: {
                typeName: 'DeliveryRuleCacheExpirationActionParameters'
                cacheBehavior: 'Override'
                cacheType: 'All'
                cacheDuration: '01:00:00'
              }
            }
          ]
        }
      ]
    }
  }
}

// ============================================================================
// Diagnostic Settings
// ============================================================================

resource backendDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'backend-diagnostics'
  scope: backendAppService
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

resource sqlDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'sql-diagnostics'
  scope: sqlDatabase
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'SQLInsights'
        enabled: true
      }
      {
        category: 'AutomaticTuning'
        enabled: true
      }
      {
        category: 'QueryStoreRuntimeStatistics'
        enabled: true
      }
      {
        category: 'QueryStoreWaitStatistics'
        enabled: true
      }
      {
        category: 'Errors'
        enabled: true
      }
      {
        category: 'DatabaseWaitStatistics'
        enabled: true
      }
      {
        category: 'Timeouts'
        enabled: true
      }
      {
        category: 'Blocks'
        enabled: true
      }
      {
        category: 'Deadlocks'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Basic'
        enabled: true
      }
      {
        category: 'InstanceAndAppAdvanced'
        enabled: true
      }
    ]
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Backend App Service URL')
output backendUrl string = 'https://${backendAppService.properties.defaultHostName}'

@description('Frontend App Service URL')
output frontendUrl string = 'https://${frontendAppService.properties.defaultHostName}'

@description('CDN Endpoint URL')
output cdnUrl string = 'https://${cdnEndpoint.properties.hostName}'

@description('SQL Server FQDN')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Key Vault URI')
output keyVaultUri string = keyVault.properties.vaultUri

@description('Application Insights Connection String')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Storage Account Name')
output storageAccountName string = storageAccount.name

@description('Resource Group Name')
output resourceGroupName string = resourceGroup().name

@description('Backend App Service Principal ID (for RBAC)')
output backendPrincipalId string = backendAppService.identity.principalId

@description('Frontend App Service Principal ID (for RBAC)')
output frontendPrincipalId string = frontendAppService.identity.principalId
