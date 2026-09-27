import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function ProtectedRoute({ children, requireRole }: { children: ReactNode; requireRole?: string }) {
  const { isAuthenticated, user } = useAuth()
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }
  if (requireRole && user?.role !== requireRole) {
    return <Navigate to="/projects" replace />
  }
  return <>{children}</>
}
