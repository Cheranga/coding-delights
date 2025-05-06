targetScope='subscription'


@description('Name of the resource group')
param name string

@description('Location where the resource group will be created')
param location string

resource newRg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: name
  location: location
}

output rgLocation string = newRg.location
