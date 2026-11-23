targetScope = 'resourceGroup'
param foundryLocation string
param foundryAccountName string
param foundryProjectName string
param applicationName string
param environmentName string
param tags object
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

resource foundry 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: foundryAccountName
  location: foundryLocation
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: foundryAccountName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    allowProjectManagement: true
    networkAcls: {
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: foundry
  name: foundryProjectName
  location: foundryLocation
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: foundryProjectName
    description: '${applicationName} ${environmentName} project'
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundry
  name: chatDeploymentName
  dependsOn: [
    foundryProject
  ]
  sku: {
    name: chatDeploymentSkuName
    capacity: chatDeploymentCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

resource voiceDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundry
  name: voiceDeploymentName
  dependsOn: [
    chatDeployment
  ]
  sku: {
    name: voiceDeploymentSkuName
    capacity: voiceDeploymentCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: voiceModelName
      version: voiceModelVersion
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

