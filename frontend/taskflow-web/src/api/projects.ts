import { httpClient } from './httpClient'
import type { Project } from '../types'

export function createProject(name: string, description: string) {
  return httpClient.post<Project>('/api/projects', { name, description }).then((r) => r.data)
}

export function addProjectMember(projectId: string, userId: string) {
  return httpClient.post(`/api/projects/${projectId}/members/${userId}`)
}
