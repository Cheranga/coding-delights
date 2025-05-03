targetScope = 'resourceGroup'

param appName string
param aspName string

var location = resourceGroup().location

resource app 'Microsoft.Web/sites@2022-03-01' = {
  name: appName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: aspName
  }
}

output prodId string = app.identity.principalId
output resourceId string = app.id