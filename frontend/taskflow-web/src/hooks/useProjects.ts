import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchProjects } from '../api/graphqlClient'
import { createProject } from '../api/projects'

export function useProjects() {
  return useQuery({ queryKey: ['projects'], queryFn: fetchProjects })
}

export function useCreateProject() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ name, description }: { name: string; description: string }) => createProject(name, description),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects'] }),
  })
}
