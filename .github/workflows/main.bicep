param appName string
param location string = resourceGroup().location
param telegram_apikey string
param onedrive_clientid string
param vg_cookie string

module storageModule 'storage.bicep' = {
  name: 'orleansStorageModule'
  params: {
    name: '${appName}storage'
    location: location
  }
}

module logsModule 'logs-and-analytics.bicep' = {
  name: 'orleansLogModule'
  params: {
    operationalInsightsName: '${appName}-logs'
    appInsightsName: '${appName}-insights'
    location: location
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2021-05-01' = {
  name: '${appName}-vnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '172.17.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: '172.17.0.0/24'
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
}

module siloModule 'app-service.bicep' = {
  name: 'orleansSiloModule'
  params: {
    appName: appName
    location: location
    vnetSubnetId: vnet.properties.subnets[0].id
    appInsightsConnectionString: logsModule.outputs.appInsightsConnectionString
    appInsightsInstrumentationKey: logsModule.outputs.appInsightsInstrumentationKey
    storageConnectionString: storageModule.outputs.connectionString
    vg_cookie: vg_cookie
    onedrive_clientid: onedrive_clientid
    telegram_apikey: telegram_apikey
  }
}
