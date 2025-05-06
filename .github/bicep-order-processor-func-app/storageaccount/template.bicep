param name string
param queues string
param blobContainers string
param tables string

var location = resourceGroup().location
var queueArray = empty(queues)? [] : split(queues, ',')
var containerArray = empty(blobContainers)? [] : split(blobContainers, ',')
var tableArray = empty(tables)? [] : split(tables, ',')

@allowed([
  'nonprod'
  'prod'
])
param storageType string = 'nonprod'

var storageSku = {
  nonprod: 'Standard_LRS'
  prod: 'Standard_GRS'
}

resource stg 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: name
  location: location
  kind: 'StorageV2'
  sku: {
    name: storageSku[storageType]
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2021-08-01' = if (!empty(queueArray)) {
  parent: stg
  name: 'default'
  resource aaa 'queues' = [for q in queueArray: {
    name: q
  }]
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-08-01' = if (!empty(containerArray)) {
  parent: stg
  name: 'default'
  resource aaa 'containers' = [for c in containerArray: {
    name: c
  }]
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01'= if (!empty(tableArray)) {
  parent: stg
  name: 'default'
  resource aaa 'tables' = [for t in tableArray: {
    name: t
  }]
}
