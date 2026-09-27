import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchProject } from '../api/graphqlClient'
import { addProjectMember } from '../api/projects'
import { assignTask, changeTaskStatus, createTask } from '../api/tasks'
import type { TaskPriority, TaskStatus } from '../types'

export function useProject(projectId: string) {
  return useQuery({
    queryKey: ['project', projectId],
    queryFn: () => fetchProject(projectId),
    enabled: Boolean(projectId),
  })
}

export function useProjectMutations(projectId: string) {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['project', projectId] })

  const createTaskMutation = useMutation({
    mutationFn: (input: { title: string; description: string; priority: TaskPriority; dueDate: string | null }) =>
      createTask(projectId, input),
    onSuccess: invalidate,
  })

  const changeStatusMutation = useMutation({
    mutationFn: ({ taskId, status }: { taskId: string; status: TaskStatus }) => changeTaskStatus(taskId, status),
    onSuccess: invalidate,
  })

  const assignMutation = useMutation({
    mutationFn: ({ taskId, assigneeId }: { taskId: string; assigneeId: string }) => assignTask(taskId, assigneeId),
    onSuccess: invalidate,
  })

  const addMemberMutation = useMutation({
    mutationFn: (userId: string) => addProjectMember(projectId, userId),
    onSuccess: invalidate,
  })

  return { createTaskMutation, changeStatusMutation, assignMutation, addMemberMutation }
}
