targetScope = 'resourceGroup'

param name string

@allowed([
    'nonprod'
    'prod'
  ]
)
param category string = 'nonprod'

var sku = {
  nonprod: 'Y1'
  prod: 'Y1'
}

var location = resourceGroup().location

resource asp 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: name
  location:location
  sku: {
    name: sku[category]
    tier: sku[category]
  }
}
