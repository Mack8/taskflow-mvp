import axios from 'axios'
import { NOTIFICATIONS_BASE_URL } from './config'
import { getToken } from './httpClient'
import type { Notification } from '../types'

const notificationsHttp = axios.create({ baseURL: NOTIFICATIONS_BASE_URL })

notificationsHttp.interceptors.request.use((config) => {
  const token = getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

export async function fetchNotifications(userId: string, signal?: AbortSignal): Promise<Notification[]> {
  const { data } = await notificationsHttp.get<Notification[]>(`/api/notifications/${userId}`, { signal })
  return data
}

export async function markNotificationRead(id: string): Promise<void> {
  await notificationsHttp.patch(`/api/notifications/${id}/read`)
}
