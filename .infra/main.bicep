targetScope = 'resourceGroup'

@description('Default Azure region used when application resources are created.')
param location string = resourceGroup().location

@description('Resolved identity region. Existing identity location is preserved by the installation script.')
param identityLocation string = location

param resourceLocations object = {}

param environmentName string
param applicationName string = 'garage-timesheet'

param appServicePlanName string
param webAppName string
param managedIdentityName string
param storageAccountName string
param logAnalyticsWorkspaceName string
param applicationInsightsName string

param appServicePlanSkuName string = 'B2'

var tags = {
  application: applicationName
  environment: environmentName
  managedBy: 'github-actions'
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: resourceLocations.?identity ?? identityLocation
  tags: tags
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: resourceLocations.?workspace ?? location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: resourceLocations.?insights ?? location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: resourceLocations.?storage ?? location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  tags: tags
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    defaultToOAuthAuthentication: true
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: resourceLocations.?plan ?? location
  kind: 'linux'
  tags: tags
  sku: {
    name: appServicePlanSkuName
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: webAppName
  location: resourceLocations.?webApp ?? location
  kind: 'app,linux'
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientCertEnabled: true
    clientCertMode: 'Required'
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
    }
  }
}

output webAppName string = webApp.name
output webAppHostName string = webApp.properties.defaultHostName
output managedIdentityClientId string = identity.properties.clientId
output managedIdentityPrincipalId string = identity.properties.principalId
output applicationInsightsConnectionString string = insights.properties.ConnectionString
