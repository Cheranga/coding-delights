targetScope = 'resourceGroup'

param appName string
param storageName string
param timeZone string = 'AUS Eastern Standard Time'

@secure()
param storageAccountConnectionStringSecretUri string

@secure()
param appInsightsKeySecretUri string

@secure()
param sbQConnectionStringSecretUri string

resource app 'Microsoft.Web/sites@2022-03-01' existing = {
  name: appName
}

var appSettings = {
  AzureWebJobsStorage__accountName: storageName  
  WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: storageAccountConnectionStringSecretUri
  WEBSITE_CONTENTSHARE: toLower(appName)
  FUNCTIONS_EXTENSION_VERSION: '~4'
  APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsKeySecretUri
  FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
  FUNCTIONS_WORKER_RUNTIME_VERSION: '8.0'
  WEBSITE_TIME_ZONE: timeZone
  WEBSITE_ADD_SITENAME_BINDINGS_IN_APPHOST_CONFIG: '1'
  SCM_DO_BUILD_DURING_DEPLOYMENT: 'true'
  StorageConfig__ProcessingQueueName: 'processing-queue'
  ServiceBusConfig__ProcessingQueueName: 'temp-order'
//   Source__Queue: 'sample-work'
//   Source__Container: 'sample-work'
//   Source__Table: 'samplework'
//   AzureWebJobsSourceConnection : storageAccountConnectionStringSecretUri
  AzureWebJobsQueueConnection: storageAccountConnectionStringSecretUri
  AzureWebJobsAsbConnection: sbQConnectionStringSecretUri
}

resource productionSlotAppSettings 'Microsoft.Web/sites/config@2021-02-01' = {
  name: 'appsettings'
  properties: appSettings
  parent: app
}
