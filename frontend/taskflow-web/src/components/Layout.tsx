import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { NotificationsBell } from './NotificationsBell'

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link to="/projects" className="brand">
          TaskFlow
        </Link>
        {user && (
          <div className="app-header-actions">
            <NotificationsBell />
            <span className="muted">{user.name}</span>
            <button className="secondary" onClick={logout}>
              Log out
            </button>
          </div>
        )}
      </header>
      <main className="app-main">{children}</main>
    </div>
  )
}
