param appName string
param location string = resourceGroup().location
param telegram_apikey string
param onedrive_clientid string
param vg_cookie string

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: toLower('${uniqueString(resourceGroup().id)}acr')
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

module env 'environment.bicep' = {
  name: 'containerAppEnvironment'
  params: {
    location: location
    operationalInsightsName: '${appName}-logs'
    appInsightsName: '${appName}-insights'
  }
}

var envVars = [
  {
    name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
    value: env.outputs.appInsightsInstrumentationKey
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: env.outputs.appInsightsConnectionString
  }
  {
    name: 'azurestorage__connectionstring'
    value: storageModule.outputs.connectionString
  }
  {
    name: 'telegram__apikey'
    value: telegram_apikey
  }
  {
    name: 'onedrive__clientId'
    value: onedrive_clientid
  }
  {
    name: 'vg__cookie'
    value: vg_cookie
  }
]

module storageModule 'storage.bicep' = {
  name: 'orleansStorageModule'
  params: {
    name: '${appName}storage'
    location: location
  }
}

module siloModule 'container-app.bicep' = {
  name: 'orleansSiloModule'
  params: {
    appName: appName
    location: location
    containerAppEnvironmentId: env.outputs.id
    registry: acr.name
    registryPassword: acr.listCredentials().passwords[0].value
    registryUsername: acr.listCredentials().username
    envVars: envVars
  }
}

output acrLoginServer string = acr.properties.loginServer
