targetScope = 'resourceGroup'

param appName string
param storageName string
param kvName string
param timeZone string = 'AUS Eastern Standard Time'

resource app 'Microsoft.Web/sites@2022-03-01' existing = {
  name: appName
}

resource kv 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: kvName
}

var appSettings = {
  AzureWebJobsStorage__accountName: storageName  
  WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}/secrets/storageAccountConnectionString/)'
  WEBSITE_CONTENTSHARE: toLower(appName)
  FUNCTIONS_EXTENSION_VERSION: '~4'
  APPINSIGHTS_INSTRUMENTATIONKEY: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}/secrets/appInsightsKey/)'
  FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
  FUNCTIONS_WORKER_RUNTIME_VERSION: '8.0'
  WEBSITE_TIME_ZONE: timeZone
  WEBSITE_ADD_SITENAME_BINDINGS_IN_APPHOST_CONFIG: '1'
  Source__Queue: 'sample-work'
  Source__Container: 'sample-work'
  Source__Table: 'samplework'
  AzureWebJobsSourceConnection : '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}/secrets/storageAccountConnectionString/)'
}

resource productionSlotAppSettings 'Microsoft.Web/sites/config@2021-02-01' = {
  name: 'appsettings'
  properties: appSettings
  parent: app
}
