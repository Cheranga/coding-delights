targetScope = 'resourceGroup'

param appId string
param kvName string
param storageName string
param appInsightsName string

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

resource storageConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  name: 'storageAccountConnectionString'
  parent: kv
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
  }
}

resource appInsightsSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  name: 'appInsightsKey'
  parent: kv
  properties: {
    value: appInsights.properties.InstrumentationKey
  }
}
