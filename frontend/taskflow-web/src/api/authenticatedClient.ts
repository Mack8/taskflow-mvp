import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios'
import { captureSession, expireSession } from '../auth/session'

export function authenticateClient(client: AxiosInstance) {
  const sessions = new WeakMap<InternalAxiosRequestConfig, ReturnType<typeof captureSession>>()
  client.interceptors.request.use((config) => {
    const session = captureSession()
    sessions.set(config, session)
    if (session.token) config.headers.Authorization = `Bearer ${session.token}`
    return config
  })
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      const session = error.config && sessions.get(error.config)
      if (error.response?.status === 401 && session) expireSession(session)
      return Promise.reject(error)
    },
  )
  return client
}
