import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadPersistedUser)

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      login: async (email, password) => {
        const result = await authApi.login(email, password)
        setUser(persist(result))
      },
      register: async (name, email, password) => {
        const result = await authApi.register(name, email, password)
        setUser(persist(result))
      },
      logout: () => {
        setToken(null)
        localStorage.removeItem(USER_KEY)
        setUser(null)
      },
    }),
    [user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
