import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createContext, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import * as authApi from '../api/auth'
import { getToken, onSessionExpired, setToken, USER_KEY } from './session'
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
  sessionExpired: boolean
  login: (email: string, password: string) => Promise<void>
  register: (name: string, email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function persist(result: AuthResult): AuthUser {
  setToken(result.token)
  const user: AuthUser = { userId: result.userId, name: result.name, email: result.email, role: result.role }
  localStorage.setItem(USER_KEY, JSON.stringify(user))
  return user
}

function loadPersistedUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY)
    const user: unknown = raw ? JSON.parse(raw) : null
    if (getToken() && typeof user === 'object' && user !== null &&
      'userId' in user && typeof user.userId === 'string' && user.userId.length > 0 &&
      'name' in user && typeof user.name === 'string' &&
      'email' in user && typeof user.email === 'string' &&
      'role' in user && (user.role === 'Admin' || user.role === 'Member')) {
      return user as AuthUser
    }
  } catch {
    // Corrupt persisted data must not prevent the login screen from rendering.
  }
  setToken(null)
  return null
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
  const [sessionExpired, setSessionExpired] = useState(false)
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
      setSessionExpired(false)
      replaceSession(persist(result))
    }

    return {
      user: session.user,
      isAuthenticated: session.user !== null,
      sessionExpired,
      login: (email, password) => authenticate(() => authApi.login(email, password)),
      register: (name, email, password) => authenticate(() => authApi.register(name, email, password)),
      logout: () => {
        setSessionExpired(false)
        ++authAttempt.current
        setToken(null)
        localStorage.removeItem(USER_KEY)
        replaceSession(null)
      },
    }
  }, [session.user, sessionExpired])

  useEffect(() => onSessionExpired(() => {
    value.logout()
    setSessionExpired(true)
  }), [value])

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
