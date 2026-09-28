import axios from 'axios'
import { API_BASE_URL } from './config'
import { authenticateClient } from './authenticatedClient'

export { getToken, setToken } from '../auth/session'

export const httpClient = authenticateClient(axios.create({ baseURL: API_BASE_URL }))
// Invalid credentials are a form error, not expiration of an existing session.
export const authHttpClient = axios.create({ baseURL: API_BASE_URL })
