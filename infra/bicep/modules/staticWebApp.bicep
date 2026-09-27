param resourceToken string
param location string
param tags object
param apiUrl string
param notificationsUrl string

// Replaces the local nginx-served taskflow-web container. Static Web Apps
// builds straight from the repo (GitHub Actions integration) rather than
// pulling a pre-built container image, which is the standard pattern for a
// Vite/React SPA with no server-side rendering.
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'stapp-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    buildProperties: {
      appLocation: 'frontend/taskflow-web'
      outputLocation: 'dist'
    }
  }
}

resource appSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    VITE_API_URL: apiUrl
    VITE_NOTIFICATIONS_URL: notificationsUrl
  }
}

output webUrl string = 'https://${staticWebApp.properties.defaultHostname}'
