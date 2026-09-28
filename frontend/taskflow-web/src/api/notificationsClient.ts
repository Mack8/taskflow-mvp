import axios from 'axios'
import { NOTIFICATIONS_BASE_URL } from './config'
import { authenticateClient } from './authenticatedClient'
import type { Notification } from '../types'

const notificationsHttp = authenticateClient(axios.create({ baseURL: NOTIFICATIONS_BASE_URL }))

export async function fetchNotifications(userId: string, signal?: AbortSignal): Promise<Notification[]> {
  const { data } = await notificationsHttp.get<Notification[]>(`/api/notifications/${userId}`, { signal })
  return data
}

export async function markNotificationRead(id: string): Promise<void> {
  await notificationsHttp.patch(`/api/notifications/${id}/read`)
}
