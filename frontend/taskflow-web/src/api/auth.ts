import { httpClient } from './httpClient'
import type { AuthResult } from '../types'

export function register(name: string, email: string, password: string) {
  return httpClient.post<AuthResult>('/api/auth/register', { name, email, password }).then((r) => r.data)
}

export function login(email: string, password: string) {
  return httpClient.post<AuthResult>('/api/auth/login', { email, password }).then((r) => r.data)
}
