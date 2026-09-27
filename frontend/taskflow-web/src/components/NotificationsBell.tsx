import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { useMarkNotificationRead, useNotifications } from '../hooks/useNotifications'

export function NotificationsBell() {
  const { user } = useAuth()
  const [open, setOpen] = useState(false)
  const { data: notifications = [] } = useNotifications(user?.userId)
  const markRead = useMarkNotificationRead(user?.userId)

  const unreadCount = notifications.filter((n) => !n.isRead).length

  return (
    <div className="notifications-bell">
      <button className="bell-button" onClick={() => setOpen((v) => !v)} aria-label="Notifications">
        🔔{unreadCount > 0 && <span className="bell-count">{unreadCount}</span>}
      </button>
      {open && (
        <div className="notifications-dropdown">
          <h4>Notifications</h4>
          {notifications.length === 0 && <p className="muted">No notifications yet.</p>}
          <ul>
            {notifications.map((n) => (
              <li key={n.id} className={n.isRead ? 'read' : 'unread'}>
                <p>{n.message}</p>
                <div className="notification-meta">
                  <span>{new Date(n.createdAt).toLocaleString()}</span>
                  {!n.isRead && (
                    <button className="link-button" onClick={() => markRead.mutate(n.id)}>
                      Mark as read
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
