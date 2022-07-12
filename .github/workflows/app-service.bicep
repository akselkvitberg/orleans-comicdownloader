param appName string
param location string
param vnetSubnetId string
param appInsightsInstrumentationKey string
param appInsightsConnectionString string
param storageConnectionString string
param telegram_apikey string
param onedrive_clientid string
param vg_cookie string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'app'
  sku: {
    name: 'S1'
    capacity: 1
  }
}

resource appService 'Microsoft.Web/sites@2021-03-01' = {
  name: appName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    virtualNetworkSubnetId: vnetSubnetId
    httpsOnly: true
    siteConfig: {
      vnetPrivatePortsCount: 2
      webSocketsEnabled: true
      netFrameworkVersion: 'v6.0'
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'azurestorage__connectionstring'
          value: storageConnectionString
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
      alwaysOn: true
    }
  }
}

resource appServiceConfig 'Microsoft.Web/sites/config@2021-03-01' = {
  name: '${appService.name}/metadata'
  properties: {
    CURRENT_STACK: 'dotnet'
  }
}
