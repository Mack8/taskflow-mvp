import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createUser } from '../api/admin'
import { fetchAllProjects, fetchUsers } from '../api/graphqlClient'
import { addProjectMember, removeProjectMember } from '../api/projects'
import type { UserRole } from '../types'

export function useUsers() {
  return useQuery({ queryKey: ['admin', 'users'], queryFn: fetchUsers })
}

export function useAllProjects() {
  return useQuery({ queryKey: ['admin', 'allProjects'], queryFn: fetchAllProjects })
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ name, email, password, role }: { name: string; email: string; password: string; role: UserRole }) =>
      createUser(name, email, password, role),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })
}

export function useAdminProjectMemberMutations() {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin', 'allProjects'] })

  const assignMutation = useMutation({
    mutationFn: ({ projectId, userId }: { projectId: string; userId: string }) => addProjectMember(projectId, userId),
    onSuccess: invalidate,
  })

  const removeMutation = useMutation({
    mutationFn: ({ projectId, userId }: { projectId: string; userId: string }) => removeProjectMember(projectId, userId),
    onSuccess: invalidate,
  })

  return { assignMutation, removeMutation }
}
