targetScope = 'resourceGroup'

@description('Default Azure region used when application resources are created.')
param location string = resourceGroup().location

@description('Resolved identity region. Existing identity location is preserved by the installation script.')
param identityLocation string = location

@description('Azure region used when the Foundry account and project are created.')
param foundryLocation string = location

param foundrySubscriptionId string = subscription().subscriptionId
param foundryResourceGroupName string = resourceGroup().name
param resourceLocations object = {}

param environmentName string
param applicationName string = 'garage-timesheet'

param appServicePlanName string
param webAppName string
param managedIdentityName string
param botAgentIdentityName string
param storageAccountName string
param logAnalyticsWorkspaceName string
param applicationInsightsName string
param foundryAccountName string
param foundryProjectName string

param appServicePlanSkuName string = 'B2'

param conversationTableName string = 'TimesheetAgentConversation'
param actionTableName string = 'TimesheetAgentAction'

param chatDeploymentName string = 'gpt-5-mini'
param chatModelName string = 'gpt-5-mini'
param chatModelVersion string = '2025-08-07'
param chatDeploymentSkuName string = 'GlobalStandard'
param chatDeploymentCapacity int = 500

param voiceDeploymentName string = 'whisper'
param voiceModelName string = 'whisper'
param voiceModelVersion string = '001'
param voiceDeploymentSkuName string = 'Standard'
param voiceDeploymentCapacity int = 3

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

resource botAgentIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: botAgentIdentityName
  location: resourceLocations.?botAgentIdentity ?? identityLocation
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

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource conversationTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: conversationTableName
}

resource actionTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: actionTableName
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

module foundryModule './foundry.bicep' = {
  name: 'timesheet-foundry-${environmentName}'
  scope: resourceGroup(foundrySubscriptionId, foundryResourceGroupName)
  params: {
    foundryLocation: foundryLocation
    foundryAccountName: foundryAccountName
    foundryProjectName: foundryProjectName
    applicationName: applicationName
    environmentName: environmentName
    tags: tags
    chatDeploymentName: chatDeploymentName
    chatModelName: chatModelName
    chatModelVersion: chatModelVersion
    chatDeploymentSkuName: chatDeploymentSkuName
    chatDeploymentCapacity: chatDeploymentCapacity
    voiceDeploymentName: voiceDeploymentName
    voiceModelName: voiceModelName
    voiceModelVersion: voiceModelVersion
    voiceDeploymentSkuName: voiceDeploymentSkuName
    voiceDeploymentCapacity: voiceDeploymentCapacity
  }
}

output webAppName string = webApp.name
output webAppHostName string = webApp.properties.defaultHostName
output managedIdentityClientId string = identity.properties.clientId
output managedIdentityPrincipalId string = identity.properties.principalId
output botAgentIdentityPrincipalId string = botAgentIdentity.properties.principalId
output botAgentIdentityClientId string = botAgentIdentity.properties.clientId
output storageTableEndpoint string = storage.properties.primaryEndpoints.table
output foundryProjectEndpoint string = 'https://${foundryAccountName}.services.ai.azure.com/api/projects/${foundryProjectName}'
output foundryOpenAiEndpoint string = 'https://${foundryAccountName}.openai.azure.com/'
output applicationInsightsConnectionString string = insights.properties.ConnectionString
