const TOKEN_KEY = 'taskflow.token'
export const USER_KEY = 'taskflow.user'
let generation = 0
const expirationListeners = new Set<() => void>()

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setToken(token: string | null) {
  ++generation
  if (token) localStorage.setItem(TOKEN_KEY, token)
  else {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
  }
}

export function captureSession() {
  return { token: getToken(), generation }
}

// Check both identity and generation: the same JWT may be issued again when
// the same account logs in twice within the token timestamp's precision.
export function expireSession(requestSession: ReturnType<typeof captureSession>) {
  if (!requestSession.token || requestSession.generation !== generation || requestSession.token !== getToken()) return
  setToken(null)
  for (const listener of expirationListeners) listener()
}

export function onSessionExpired(listener: () => void) {
  expirationListeners.add(listener)
  return () => { expirationListeners.delete(listener) }
}
