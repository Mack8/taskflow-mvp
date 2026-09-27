// Mirrors the DTOs returned by TaskFlow.Api (see backend/src/TaskFlow.Application/DTOs).

export type TaskStatus = 'Todo' | 'InProgress' | 'InReview' | 'Done'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export interface AuthResult {
  userId: string
  name: string
  email: string
  role: string
  token: string
}

export type UserRole = 'Member' | 'Admin'

export interface UserSummary {
  id: string
  name: string
  email: string
  role: UserRole
  createdAt: string
}

export interface Project {
  id: string
  name: string
  description: string
  ownerId: string
  memberCount: number
  taskCount: number
  createdAt: string
}

export interface ProjectDetail {
  id: string
  name: string
  description: string
  ownerId: string
  memberIds: string[]
  tasks: Task[]
}

export interface Task {
  id: string
  projectId: string
  title: string
  description: string
  status: TaskStatus
  priority: TaskPriority
  assigneeId: string | null
  dueDate: string | null
  createdAt: string
  updatedAt: string | null
}

export interface Notification {
  id: string
  userId: string
  type: string
  message: string
  relatedProjectId: string | null
  relatedTaskId: string | null
  isRead: boolean
  createdAt: string
}

export interface ApiErrorPayload {
  title: string
  status: number
  errors?: Record<string, string[]> | null
}
