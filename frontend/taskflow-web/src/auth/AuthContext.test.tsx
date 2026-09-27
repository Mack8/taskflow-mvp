import { useEffect } from 'react'
import { act, cleanup, render, renderHook, screen, waitFor } from '@testing-library/react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider, useAuth } from './AuthContext'
import * as authApi from '../api/auth'
import { fetchProjects, fetchUsers } from '../api/graphqlClient'
import { useProjects } from '../hooks/useProjects'
import { useUsers } from '../hooks/useAdmin'
import { getToken } from '../api/httpClient'
import type { AuthResult, Project, UserSummary } from '../types'

vi.mock('../api/auth', () => ({ login: vi.fn(), register: vi.fn() }))
vi.mock('../api/graphqlClient', () => ({ fetchProjects: vi.fn(), fetchUsers: vi.fn(), fetchAllProjects: vi.fn() }))
function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((done) => { resolve = done })
  return { promise, resolve }
}
const account = (userId: string, role = 'Member'): AuthResult => ({
  userId, role, name: userId, email: `${userId}@example.test`, token: `token-${userId}`,
})
const useSession = () => ({ auth: useAuth(), client: useQueryClient() })
beforeEach(() => { localStorage.clear(); vi.resetAllMocks() })
afterEach(() => { cleanup(); localStorage.clear() })
function Projects() {
  const { data } = useProjects()
  return <div>{data?.map((p) => <span key={p.id}>{p.name}</span>)}</div>
}
function Users() {
  const { data } = useUsers()
  return <div>{data?.map((u) => <span key={u.id}>{u.name}</span>)}</div>
}

describe('session isolation', () => {
  it('hides cached projects and admin data when another account logs in', async () => {
    let auth!: ReturnType<typeof useAuth>
    function View() {
      const current = useAuth()
      useEffect(() => { auth = current }, [current])
      return current.user ? <><Projects />{current.user.role === 'Admin' && <Users />}</> : null
    }
    vi.mocked(fetchProjects).mockResolvedValue([{ id: 'private-project', name: 'Private A project' }] as Project[])
    vi.mocked(fetchUsers).mockResolvedValue([{ id: 'private-user', name: 'Private directory entry' }] as UserSummary[])
    vi.mocked(authApi.login).mockResolvedValueOnce(account('A', 'Admin')).mockResolvedValueOnce(account('B'))
    render(<AuthProvider><View /></AuthProvider>)
    await act(async () => { await auth.login('A', 'password') })
    expect(await screen.findByText('Private A project')).toBeInTheDocument()
    expect(await screen.findByText('Private directory entry')).toBeInTheDocument()
    vi.mocked(fetchProjects).mockReturnValue(deferred<Project[]>().promise)
    act(() => auth.logout())
    await act(async () => { await auth.login('B', 'password') })
    expect(screen.queryByText('Private A project')).not.toBeInTheDocument()
    expect(screen.queryByText('Private directory entry')).not.toBeInTheDocument()
    expect(fetchProjects).toHaveBeenCalledTimes(2)
    expect(fetchUsers).toHaveBeenCalledTimes(1)
    const signal = vi.mocked(fetchProjects).mock.calls[1][0]
    expect(signal).toBeInstanceOf(AbortSignal)
    act(() => auth.logout())
    expect(signal?.aborted).toBe(true)
  })

  it('cancels old queries and isolates late responses even when transport ignores abort', async () => {
    const { result } = renderHook(useSession, { wrapper: AuthProvider })
    vi.mocked(authApi.login).mockResolvedValueOnce(account('A')).mockResolvedValueOnce(account('B'))
    await act(async () => { await result.current.auth.login('A', 'password') })
    const previous = result.current.client
    const pending = deferred<string>()
    let signal!: AbortSignal
    const request = previous.fetchQuery({ queryKey: ['projects'], queryFn: (context) => {
      signal = context.signal
      return pending.promise
    } }).catch(() => undefined)
    act(() => result.current.auth.logout())
    await act(async () => { await result.current.auth.login('B', 'password') })
    expect(signal.aborted).toBe(true)
    act(() => result.current.client.setQueryData(['projects'], 'B data'))
    await act(async () => { pending.resolve('A data'); await request })
    expect(result.current.client.getQueryData(['projects'])).toBe('B data')
    expect(previous.getQueryCache().getAll()).toHaveLength(0)
  })

  it('isolates late mutation callbacks and clears the old mutation cache', async () => {
    const pending = deferred<string>()
    const { result } = renderHook(() => {
      const client = useQueryClient()
      return { auth: useAuth(), client, mutation: useMutation({
        mutationFn: () => pending.promise,
        onSuccess: (data) => { client.setQueryData(['projects'], data) },
      }) }
    }, { wrapper: AuthProvider })
    vi.mocked(authApi.login).mockResolvedValueOnce(account('A')).mockResolvedValueOnce(account('B'))
    await act(async () => { await result.current.auth.login('A', 'password') })
    const previous = result.current.client
    let mutation!: Promise<string>
    act(() => { mutation = result.current.mutation.mutateAsync() })
    await waitFor(() => expect(result.current.mutation.isPending).toBe(true))
    act(() => result.current.auth.logout())
    await act(async () => { await result.current.auth.login('B', 'password') })
    act(() => result.current.client.setQueryData(['projects'], 'B data'))
    await act(async () => { pending.resolve('A mutation result'); await mutation })
    expect(result.current.client.getQueryData(['projects'])).toBe('B data')
    expect(result.current.client.getMutationCache().getAll()).toHaveLength(0)
    expect(previous.getMutationCache().getAll()).toHaveLength(0)
  })

  it.each(['login', 'register'] as const)('does not restore a session after pending %s and logout', async (method) => {
    const pending = deferred<AuthResult>()
    vi.mocked(authApi[method]).mockReturnValue(pending.promise)
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    let request!: Promise<unknown>
    act(() => {
      request = (method === 'login' ? result.current.login('A', 'password') : result.current.register('A', 'A', 'password'))
        .catch((error: unknown) => error)
    })
    act(() => result.current.logout())
    let error: unknown
    await act(async () => { pending.resolve(account('A')); error = await request })
    expect(error).toMatchObject({ name: 'AbortError' })
    expect(result.current.isAuthenticated).toBe(false)
    expect(getToken()).toBeNull()
    expect(localStorage.getItem('taskflow.user')).toBeNull()
  })

  it('keeps the latest login when an earlier login finishes last', async () => {
    const first = deferred<AuthResult>()
    vi.mocked(authApi.login).mockReturnValueOnce(first.promise).mockResolvedValueOnce(account('B'))
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    let oldRequest!: Promise<unknown>
    act(() => { oldRequest = result.current.login('A', 'password').catch((error: unknown) => error) })
    await act(async () => { await result.current.login('B', 'password') })
    await act(async () => { first.resolve(account('A')); await oldRequest })
    expect(result.current.user?.userId).toBe('B')
    expect(getToken()).toBe('token-B')
  })

  it('creates a fresh cache when signing back into the same account', async () => {
    vi.mocked(authApi.login).mockResolvedValue(account('A'))
    const { result } = renderHook(useSession, { wrapper: AuthProvider })
    await act(async () => { await result.current.auth.login('A', 'password') })
    const previous = result.current.client
    previous.setQueryData(['admin', 'allProjects'], ['private'])
    await act(async () => { await result.current.auth.login('A', 'password') })
    expect(result.current.client).not.toBe(previous)
    expect(result.current.client.getQueryCache().getAll()).toHaveLength(0)
    expect(previous.getQueryCache().getAll()).toHaveLength(0)
  })
})
