import axios from 'axios'
import { API_BASE_URL } from './config'

export const httpClient = axios.create({ baseURL: API_BASE_URL })

const TOKEN_KEY = 'taskflow.token'

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token)
  else localStorage.removeItem(TOKEN_KEY)
}

httpClient.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// A 401 here means the token expired or was rejected — not that this one
// request failed for business reasons. Clearing it and bouncing to /login is
// safe because ProtectedRoute treats "no token" as the single source of truth
// for auth state.
httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      setToken(null)
      window.location.href = '/login'
    }
    return Promise.reject(error)
  },
)
