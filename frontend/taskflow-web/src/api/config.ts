// Both services are independently deployable microservices with their own base
// URL — in docker-compose they're reverse-proxied to distinct ports; in Azure
// each maps to its own App Service (see infra/bicep).
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'
export const NOTIFICATIONS_BASE_URL = import.meta.env.VITE_NOTIFICATIONS_URL ?? 'http://localhost:5081'
export const GRAPHQL_URL = `${API_BASE_URL}/graphql`
