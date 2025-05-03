targetScope = 'resourceGroup'

param friendlyName string
param appId string
param storageName string

var blobDataOwner = guid('${resourceGroup().name}-${friendlyName}-blobowner')
var queueContributor = guid('${resourceGroup().name}-${friendlyName}-queuecont')
var storageAccContributor = guid('${resourceGroup().name}-${friendlyName}-stgacccont')


resource storageBlobDataOwner 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
}

resource storageQueueDataContributor 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
}

resource storageAccountContributor 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '17d1049b-9a84-46fb-8f53-869881c3d3ab'
}

resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageName
}

resource rbacStorageBlobDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: blobDataOwner
  scope: storage
  properties: {
    principalId: appId
    principalType:'ServicePrincipal'
    roleDefinitionId: storageBlobDataOwner.id
  }
}

resource rbacStorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: queueContributor
  scope: storage
  properties: {
    principalId: appId
    principalType:'ServicePrincipal'
    roleDefinitionId: storageQueueDataContributor.id
  }
}

resource rbacStorageAccountContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: storageAccContributor
  scope: storage
  properties: {
    principalId: appId
    principalType:'ServicePrincipal'
    roleDefinitionId: storageAccountContributor.id
  }
}