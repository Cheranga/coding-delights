targetScope = 'subscription'

param appName string
param version string
param location string


@allowed([
  'DEV'
  'QA',
  'PROD'
])
param environment string

var envType = {
    DEV: 'nonprod'
    QA: 'nonprod'
    PROD: 'prod'
    } 

var appNameWithEnvironment = '${appName}-${environment}'
var rgName = 'cchat-rg-${appNameWithEnvironment}'
var funcAppName = 'cchat-fn-${appNameWithEnvironment}'
var sgName = take(replace('cchatsg${appNameWithEnvironment}', '-', ''), 24)
var appInsName = 'cchat-ins-${appNameWithEnvironment}'
var aspName = 'cchat-asp-${appNameWithEnvironment}'
var kvName = take(replace('cchatkv${appNameWithEnvironment}', '-', ''), 24)

module rg 'resourcegroup/template.bicep' = {
  scope: subscription()
  name: '${version}-rg'
  params: {
    location: location
    name: rgName
  }
}