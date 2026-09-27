import axios, { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { setToken } from './httpClient'

afterEach(() => {
  setToken(null)
  vi.restoreAllMocks()
})

describe('notifications authentication', () => {
  it('uses the current session token for reads and updates and removes it after logout', async () => {
    const requests: InternalAxiosRequestConfig[] = []
    const client = axios.create({
      adapter: async (config) => {
        requests.push(config)
        return { data: [], status: 200, statusText: 'OK', headers: new AxiosHeaders(), config }
      },
    })
    vi.spyOn(axios, 'create').mockReturnValue(client)
    const { fetchNotifications, markNotificationRead } = await import('./notificationsClient')

    setToken('first-session')
    await fetchNotifications('user-1')
    setToken('second-session')
    await markNotificationRead('notification-1')
    setToken(null)
    await fetchNotifications('user-2')

    expect(requests[0].headers.Authorization).toBe('Bearer first-session')
    expect(requests[1].headers.Authorization).toBe('Bearer second-session')
    expect(requests[2].headers.Authorization).toBeUndefined()
  })
})
