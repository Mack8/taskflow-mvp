param resourceToken string
param location string
param tags object

// Cosmos DB's MongoDB API is the drop-in replacement for the local `mongo`
// container: TaskFlow.Notifications talks to it through the same
// MongoDB.Driver client and connection-string shape, no code changes.
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: 'cosmos-${resourceToken}'
  location: location
  tags: tags
  kind: 'MongoDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    apiProperties: {
      serverVersion: '7.0'
    }
    capabilities: [
      { name: 'EnableServerless' }
    ]
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
}

resource mongoDatabase 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: 'taskflow_notifications'
  properties: {
    resource: {
      id: 'taskflow_notifications'
    }
  }
}

output connectionString string = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
