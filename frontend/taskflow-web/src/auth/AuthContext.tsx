import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createContext, useContext, useMemo, useRef, useState, type ReactNode } from 'react'
import * as authApi from '../api/auth'
import { getToken, setToken } from '../api/httpClient'
import type { AuthResult } from '../types'

interface AuthUser {
  userId: string
  name: string
  email: string
  role: string
}

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (name: string, email: string, password: string) => Promise<void>
  logout: () => void
}

const USER_KEY = 'taskflow.user'
const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function persist(result: AuthResult): AuthUser {
  setToken(result.token)
  const user: AuthUser = { userId: result.userId, name: result.name, email: result.email, role: result.role }
  localStorage.setItem(USER_KEY, JSON.stringify(user))
  return user
}

function loadPersistedUser(): AuthUser | null {
  if (!getToken()) return null
  const raw = localStorage.getItem(USER_KEY)
  return raw ? (JSON.parse(raw) as AuthUser) : null
}

function createSession(user: AuthUser | null, id: number) {
  return {
    user,
    id,
    queryClient: new QueryClient({
      defaultOptions: { queries: { retry: 1, staleTime: 10_000 } },
    }),
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState(() => createSession(loadPersistedUser(), 0))
  const activeSession = useRef(session)
  const authAttempt = useRef(0)

  const value = useMemo<AuthContextValue>(() => {
    function replaceSession(user: AuthUser | null) {
      const previous = activeSession.current
      // Cancellation is best effort. A separate client and remounted observers
      // also isolate responses/callbacks from transports that ignore cancellation.
      void previous.queryClient.cancelQueries()
      previous.queryClient.clear()
      const next = createSession(user, previous.id + 1)
      activeSession.current = next
      setSession(next)
    }

    async function authenticate(request: () => Promise<AuthResult>) {
      const attempt = ++authAttempt.current
      const result = await request()
      if (attempt !== authAttempt.current) {
        throw new DOMException('Authentication attempt superseded', 'AbortError')
      }
      replaceSession(persist(result))
    }

    return {
      user: session.user,
      isAuthenticated: session.user !== null,
      login: (email, password) => authenticate(() => authApi.login(email, password)),
      register: (name, email, password) => authenticate(() => authApi.register(name, email, password)),
      logout: () => {
        ++authAttempt.current
        setToken(null)
        localStorage.removeItem(USER_KEY)
        replaceSession(null)
      },
    }
  }, [session.user])

  return (
    <AuthContext.Provider value={value}>
      <QueryClientProvider key={session.id} client={session.queryClient}>
        {children}
      </QueryClientProvider>
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
