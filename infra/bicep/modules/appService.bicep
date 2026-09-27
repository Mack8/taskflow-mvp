param resourceToken string
param location string
param tags object
param containerRegistryServer string
param imageTag string

@secure()
param sqlConnectionString string

@secure()
param cosmosMongoConnectionString string

@secure()
param serviceBusConnectionString string

@secure()
param jwtSecretVaultUri string
param appInsightsConnectionString string
param keyVaultName string

// Linux Consumption-friendly plan: both services are stateless HTTP APIs, so
// they scale out horizontally on the same plan rather than needing dedicated
// compute per service.
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'plan-${resourceToken}'
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-${resourceToken}-api'
  location: location
  tags: tags
  kind: 'app,linux,container'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerRegistryServer}/taskflow-mvp/taskflow-api:${imageTag}'
      appSettings: [
        { name: 'ConnectionStrings__SqlServer', value: sqlConnectionString }
        { name: 'ServiceBus__ConnectionString', value: serviceBusConnectionString }
        { name: 'Jwt__SecretKey', value: '@Microsoft.KeyVault(SecretUri=${jwtSecretVaultUri})' }
        { name: 'ApplicationInsights__ConnectionString', value: appInsightsConnectionString }
        { name: 'WEBSITES_PORT', value: '8080' }
      ]
    }
    httpsOnly: true
  }
}

resource notificationsApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-${resourceToken}-notifications'
  location: location
  tags: tags
  kind: 'app,linux,container'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerRegistryServer}/taskflow-mvp/taskflow-notifications:${imageTag}'
      appSettings: [
        { name: 'Mongo__ConnectionString', value: cosmosMongoConnectionString }
        { name: 'ServiceBus__ConnectionString', value: serviceBusConnectionString }
        { name: 'ApplicationInsights__ConnectionString', value: appInsightsConnectionString }
        { name: 'WEBSITES_PORT', value: '8080' }
      ]
    }
    httpsOnly: true
  }
}

// Both App Services read secrets via managed identity instead of a
// connection string with an embedded Key Vault access key.
resource keyVaultRef 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource apiKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultRef.id, apiApp.id, 'secrets-user')
  scope: keyVaultRef
  properties: {
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
  }
}

output apiUrl string = 'https://${apiApp.properties.defaultHostName}'
output notificationsUrl string = 'https://${notificationsApp.properties.defaultHostName}'
