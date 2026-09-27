import { httpClient } from './httpClient'
import type { Task, TaskPriority, TaskStatus } from '../types'

export function createTask(
  projectId: string,
  input: { title: string; description: string; priority: TaskPriority; dueDate: string | null },
) {
  return httpClient.post<Task>(`/api/projects/${projectId}/tasks`, input).then((r) => r.data)
}

export function changeTaskStatus(taskId: string, newStatus: TaskStatus) {
  return httpClient.patch<Task>(`/api/tasks/${taskId}/status`, { taskId, newStatus }).then((r) => r.data)
}

export function assignTask(taskId: string, assigneeId: string) {
  return httpClient.patch<Task>(`/api/tasks/${taskId}/assign`, { taskId, assigneeId }).then((r) => r.data)
}
