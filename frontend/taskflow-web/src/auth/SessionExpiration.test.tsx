import { act, cleanup, fireEvent, render, renderHook, screen } from '@testing-library/react'
import { useQueryClient } from '@tanstack/react-query'
import { AxiosError, AxiosHeaders } from 'axios'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider, useAuth } from './AuthContext'
import { captureSession, expireSession, getToken, onSessionExpired, setToken, USER_KEY } from './session'
import { httpClient } from '../api/httpClient'
import { fetchProjects } from '../api/graphqlClient'
import { fetchNotifications, markNotificationRead } from '../api/notificationsClient'
import { login } from '../api/auth'
import { ProtectedRoute } from './ProtectedRoute'
import { LoginPage } from '../pages/LoginPage'

const transport = vi.hoisted(() => vi.fn())
vi.mock('axios', async (importOriginal) => {
  const actual = await importOriginal<typeof import('axios')>()
  return { ...actual, default: { ...actual.default,
    create: (config: object) => actual.default.create({ ...config, adapter: transport }),
  } }
})
const graphqlTransport = vi.hoisted(() => vi.fn<typeof fetch>())
vi.mock('graphql-request', async (importOriginal) => {
  const actual = await importOriginal<typeof import('graphql-request')>()
  return { ...actual, GraphQLClient: class extends actual.GraphQLClient {
    constructor(...[url, config]: ConstructorParameters<typeof actual.GraphQLClient>) {
      super(url, { ...config, fetch: graphqlTransport })
    }
  } }
})

const user = { userId: 'A', name: 'Account A', email: 'a@example.test', role: 'Member' }
function persist(token = 'token-A') {
  setToken(token)
  localStorage.setItem(USER_KEY, JSON.stringify(user))
}
function rejectHttp(status: number) {
  transport.mockImplementation(async (config) => {
    throw new AxiosError('Rejected', 'ERR_BAD_REQUEST', config, undefined, {
      config, data: null, headers: new AxiosHeaders(), status, statusText: 'Rejected',
    })
  })
}
function rejectGraphql(status: number, code?: string) {
  graphqlTransport.mockResolvedValue(new Response(JSON.stringify({
    errors: [{ message: 'Rejected', extensions: { code } }],
  }), { status, headers: { 'Content-Type': 'application/json' } }))
}
const calls = [
  ['REST', () => httpClient.get('/api/projects')],
  ['GraphQL', () => fetchProjects()],
  ['notifications read', () => fetchNotifications('A')],
  ['notifications update', () => markNotificationRead('notification')],
] as const
const useSession = () => ({ auth: useAuth(), client: useQueryClient() })

beforeEach(() => {
  vi.resetAllMocks()
  localStorage.clear()
  persist()
  rejectHttp(401)
  rejectGraphql(401)
})
afterEach(() => { cleanup(); setToken(null) })

describe('shared session expiration', () => {
  it.each(calls)('%s clears the user, token and private caches on 401', async (_, request) => {
    const { result } = renderHook(useSession, { wrapper: AuthProvider })
    const previous = result.current.client
    previous.setQueryData(['private'], 'secret')
    await act(async () => { await request().catch(() => undefined) })
    expect(result.current.auth.isAuthenticated).toBe(false)
    expect(result.current.auth.sessionExpired).toBe(true)
    expect(getToken()).toBeNull()
    expect(localStorage.getItem(USER_KEY)).toBeNull()
    expect(previous.getQueryCache().getAll()).toHaveLength(0)
    expect(result.current.client).not.toBe(previous)
  })

  it('handles simultaneous 401 responses only once', async () => {
    const expired = vi.fn()
    const unsubscribe = onSessionExpired(expired)
    const { result } = renderHook(useSession, { wrapper: AuthProvider })
    try {
      await act(async () => { await Promise.all(calls.map(([, request]) => request().catch(() => undefined))) })
      expect(expired).toHaveBeenCalledTimes(1)
      expect(result.current.auth.sessionExpired).toBe(true)
    } finally { unsubscribe() }
  })

  it.each(calls)('%s keeps a valid session on 403', async (_, request) => {
    rejectHttp(403)
    rejectGraphql(403)
    const { result } = renderHook(useSession, { wrapper: AuthProvider })
    const previous = result.current.client
    await act(async () => { await request().catch(() => undefined) })
    expect(result.current.auth.user?.userId).toBe('A')
    expect(result.current.auth.sessionExpired).toBe(false)
    expect(result.current.client).toBe(previous)
  })

  it.each(['AUTH_NOT_AUTHORIZED', 'BUSINESS_ERROR'])('does not expire on GraphQL %s at HTTP 200', async (code) => {
    rejectGraphql(200, code)
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    await act(async () => { await fetchProjects().catch(() => undefined) })
    expect(result.current.isAuthenticated).toBe(true)
    expect(getToken()).toBe('token-A')
  })

  it('expires on the explicit GraphQL unauthenticated error at HTTP 200', async () => {
    rejectGraphql(200, 'AUTH_NOT_AUTHENTICATED')
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    await act(async () => { await expect(fetchProjects()).rejects.toMatchObject({ response: { status: 200 } }) })
    expect(result.current.isAuthenticated).toBe(false)
  })

  it('does not log out or send a bearer token for invalid login credentials', async () => {
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    await act(async () => { await login('wrong', 'wrong').catch(() => undefined) })
    expect(transport.mock.calls[0][0].headers.Authorization).toBeUndefined()
    expect(result.current.isAuthenticated).toBe(true)
    expect(result.current.sessionExpired).toBe(false)
  })

  it.each(calls)('%s ignores a late 401 from a previous session even with an identical JWT', async (_, request) => {
    let release!: () => void
    let started!: () => void
    const ready = new Promise<void>((resolve) => { started = resolve })
    const pending = new Promise<void>((resolve) => { release = resolve })
    transport.mockImplementation(async (config) => {
      started()
      await pending
      throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, undefined, {
        config, data: null, headers: new AxiosHeaders(), status: 401, statusText: 'Unauthorized',
      })
    })
    graphqlTransport.mockImplementation(async () => {
      started()
      await pending
      return new Response('{}', { status: 401, headers: { 'Content-Type': 'application/json' } })
    })
    const expired = vi.fn()
    const unsubscribe = onSessionExpired(expired)
    try {
      const response = request().catch(() => undefined)
      await ready
      persist('token-A')
      release()
      await response
      expect(getToken()).toBe('token-A')
      expect(expired).not.toHaveBeenCalled()
    } finally { unsubscribe() }
  })

  it('ignores an expired snapshot after login reuses the same token', () => {
    const previous = captureSession()
    setToken('token-A')
    expireSession(previous)
    expect(getToken()).toBe('token-A')
  })

  it.each(['{broken', 'null', '[]', '{}', '{"userId":12}', '{"userId":"A","name":"A","email":"a","role":"Unknown"}'])
    ('loads safely with malformed persisted user %s', (raw) => {
      localStorage.setItem(USER_KEY, raw)
      const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
      expect(result.current.isAuthenticated).toBe(false)
      expect(getToken()).toBeNull()
      expect(localStorage.getItem(USER_KEY)).toBeNull()
    })

  it('redirects a protected page to login with an expiration message', async () => {
    render(<MemoryRouter initialEntries={['/projects']}><AuthProvider><Routes>
      <Route path="/projects" element={<ProtectedRoute><p>Private content</p></ProtectedRoute>} />
      <Route path="/login" element={<LoginPage />} />
    </Routes></AuthProvider></MemoryRouter>)
    expect(screen.getByText('Private content')).toBeInTheDocument()
    await act(async () => { await fetchNotifications('A').catch(() => undefined) })
    expect(await screen.findByRole('heading', { name: 'Log in to TaskFlow' })).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Your session has expired. Please log in again.')
    expect(screen.queryByText('Private content')).not.toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'a@example.test' } })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'wrong' } })
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }))
    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Log in to TaskFlow' })).toBeInTheDocument()

    transport.mockImplementation(async (config) => ({
      config, data: { ...user, token: 'fresh-token' }, headers: new AxiosHeaders(), status: 200, statusText: 'OK',
    }))
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }))
    expect(await screen.findByText('Private content')).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(getToken()).toBe('fresh-token')
  })
})
