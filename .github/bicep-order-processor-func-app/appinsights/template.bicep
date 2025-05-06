targetScope = 'resourceGroup'

param name string

var location = resourceGroup().location

resource appIns 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    Flow_Type: 'Bluefield'
  }
}

output appInsKey string = appIns.properties.InstrumentationKey
