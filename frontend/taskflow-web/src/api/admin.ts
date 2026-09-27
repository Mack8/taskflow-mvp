import { httpClient } from './httpClient'
import type { UserRole, UserSummary } from '../types'

export function createUser(name: string, email: string, password: string, role: UserRole) {
  return httpClient.post<UserSummary>('/api/admin/users', { name, email, password, role }).then((r) => r.data)
}
