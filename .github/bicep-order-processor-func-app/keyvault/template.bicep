targetScope = 'resourceGroup'

param name string

var location = resourceGroup().location

resource kv 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: name
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    enabledForDeployment: true
    enabledForDiskEncryption: true
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    tenantId: subscription().tenantId
    accessPolicies: []
  }
}

output keyVaultUri string = kv.properties.vaultUri
