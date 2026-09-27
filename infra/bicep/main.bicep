// TaskFlow — Azure PaaS topology.
//
// Maps 1:1 onto the local docker-compose stack:
//   sqlserver          -> Azure SQL Database              (modules/sql.bicep)
//   mongo              -> Cosmos DB for MongoDB            (modules/cosmosMongo.bicep)
//   rabbitmq           -> Service Bus namespace + topic    (modules/serviceBus.bicep)
//   taskflow-api        -> App Service (Linux container)   (modules/appService.bicep)
//   taskflow-notifications -> App Service (Linux container)(modules/appService.bicep)
//   taskflow-web        -> Static Web App                  (modules/staticWebApp.bicep)
//
// Secrets (SQL admin password, JWT signing key, connection strings) live in
// Key Vault, referenced by the App Services via Key Vault references rather
// than being written into App Settings directly.
//
// This is a design artifact, not a deployed environment: `az deployment
// group create` against these files is the only step this repo doesn't run,
// since it costs money and needs a subscription this MVP doesn't assume.
//
// `az bicep build` passes but flags outputs-should-not-contain-secrets on the
// SQL/Cosmos/Service Bus connection strings threaded between modules — a real
// finding, kept for readability here. Hardening it means each of those
// modules writes its own secret into Key Vault and returns only a secret URI,
// the way modules/keyVault.bicep + the JWT key already do for appService.bicep.

targetScope = 'resourceGroup'

@description('Short name used as a prefix for every resource, e.g. "taskflow".')
param appName string = 'taskflow'

@description('Deployment environment, used in resource names and tags.')
@allowed(['dev', 'staging', 'prod'])
param environmentName string = 'dev'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Container registry hostname images are pulled from (e.g. GHCR or ACR login server).')
param containerRegistryServer string = 'ghcr.io'

@description('Image tag to deploy for both App Services.')
param imageTag string = 'latest'

@secure()
@description('SQL Server administrator password.')
param sqlAdminPassword string

@secure()
@description('Symmetric signing key for JWTs issued by TaskFlow.Api.')
param jwtSecretKey string

var resourceToken = '${appName}-${environmentName}'
var tags = {
  application: 'taskflow'
  environment: environmentName
}

module logAnalytics 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
    sqlAdminPassword: sqlAdminPassword
    jwtSecretKey: jwtSecretKey
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
    sqlAdminPassword: sqlAdminPassword
  }
}

module cosmosMongo 'modules/cosmosMongo.bicep' = {
  name: 'cosmosMongo'
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
  }
}

module serviceBus 'modules/serviceBus.bicep' = {
  name: 'serviceBus'
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
  }
}

module appService 'modules/appService.bicep' = {
  name: 'appService'
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
    containerRegistryServer: containerRegistryServer
    imageTag: imageTag
    sqlConnectionString: sql.outputs.connectionString
    cosmosMongoConnectionString: cosmosMongo.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    jwtSecretVaultUri: keyVault.outputs.jwtSecretUri
    appInsightsConnectionString: logAnalytics.outputs.appInsightsConnectionString
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

module staticWebApp 'modules/staticWebApp.bicep' = {
  name: 'staticWebApp'
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
    apiUrl: appService.outputs.apiUrl
    notificationsUrl: appService.outputs.notificationsUrl
  }
}

output apiUrl string = appService.outputs.apiUrl
output notificationsUrl string = appService.outputs.notificationsUrl
output webUrl string = staticWebApp.outputs.webUrl
