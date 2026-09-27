param resourceToken string
param location string
param tags object

@secure()
param sqlAdminPassword string

@secure()
param jwtSecretKey string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kv-${replace(resourceToken, '-', '')}', 24)
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
  }
}

resource sqlAdminSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-admin-password'
  properties: {
    value: sqlAdminPassword
  }
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-secret-key'
  properties: {
    value: jwtSecretKey
  }
}

output keyVaultName string = keyVault.name
output jwtSecretUri string = jwtSecret.properties.secretUri
