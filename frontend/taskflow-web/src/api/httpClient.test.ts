import { AxiosError, AxiosHeaders, type AxiosAdapter } from 'axios'
import { afterEach, expect, it } from 'vitest'
import { getToken, httpClient, setToken } from './httpClient'

const originalAdapter = httpClient.defaults.adapter
afterEach(() => {
  httpClient.defaults.adapter = originalAdapter
  setToken(null)
})

it('does not clear a new token when an old session request returns 401', async () => {
  let rejectRequest!: () => void
  let started!: () => void
  const ready = new Promise<void>((resolve) => { started = resolve })
  const adapter: AxiosAdapter = (config) => new Promise((_, reject) => {
    rejectRequest = () => reject(new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, undefined, {
      config, data: null, headers: new AxiosHeaders(), status: 401, statusText: 'Unauthorized',
    }))
    started()
  })
  httpClient.defaults.adapter = adapter
  setToken('old-token')
  const request = httpClient.get('/api/projects').catch((error: unknown) => error)
  await ready
  setToken('new-token')
  rejectRequest()
  expect(await request).toBeInstanceOf(AxiosError)
  expect(getToken()).toBe('new-token')
})
