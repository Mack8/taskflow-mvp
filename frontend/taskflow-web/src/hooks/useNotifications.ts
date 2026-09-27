import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchNotifications, markNotificationRead } from '../api/notificationsClient'

// Polling (not a socket) is the deliberate choice for the MVP: the Notifications
// microservice is a plain REST API, so the client just re-fetches on an
// interval — trading a little latency for not needing SignalR/WebSockets
// wired through both services and the Azure topology.
export function useNotifications(userId: string | undefined) {
  return useQuery({
    queryKey: ['notifications', userId],
    queryFn: () => fetchNotifications(userId!),
    enabled: Boolean(userId),
    refetchInterval: 15_000,
  })
}

export function useMarkNotificationRead(userId: string | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications', userId] }),
  })
}
