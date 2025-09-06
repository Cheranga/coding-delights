targetScope = 'resourceGroup'

param appId string
param kvName string
param storageName string
param appInsightsName string

@description('Name of the resource group where the service bus namespace is provisioned')
param sbRGName string

@description('Service bus namespace')
param sbNamespace string

@description('Name of the service bus queue')
param sbQName string

@description('Name of the authorization rule to be used on the queue')
param sbQAuthRuleName string

resource kvPolicies 'Microsoft.KeyVault/vaults/accessPolicies@2022-07-01' = {
  name: 'replace'
  properties: {
    accessPolicies: [
      {
        objectId: appId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
        tenantId: subscription().tenantId
      }
    ]
  }
  parent: kv
}

resource kv 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: kvName
}

resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource sbQueueAuthRule 'Microsoft.ServiceBus/namespaces/queues/authorizationRules@2024-01-01' existing = {
  name: '${sbNamespace}/${sbQName}/${sbQAuthRuleName}'
  scope: resourceGroup(subscription().subscriptionId, sbRGName)
}

resource storageConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  name: 'storageAccountConnectionString'
  parent: kv
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
  }
}

resource appInsightsSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  name: 'appInsightsConnectionString'
  parent: kv
  properties: {
    value: appInsights.properties.ConnectionString
  }
}

resource qConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  name: 'qConnectionString'
  parent: kv
  properties: {
    value: sbQueueAuthRule.listKeys().primaryConnectionString
  }
}

output appInsightsSecretUri string = '${kv.properties.vaultUri}/secrets/${appInsightsSecret.name}/'
output storageConnectionSecretUri string = '${kv.properties.vaultUri}/secrets/${storageConnectionSecret.name}/'
output sbQConnectionSecretUri string = '${kv.properties.vaultUri}/secrets/${qConnectionStringSecret.name}/'
