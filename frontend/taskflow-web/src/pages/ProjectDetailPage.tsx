import { useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { StatusBadge } from '../components/StatusBadge'
import { TaskCard } from '../components/TaskCard'
import { mutationErrorMessage } from '../api/errorMessage'
import { useAuth } from '../auth/AuthContext'
import { useProject, useProjectMutations } from '../hooks/useProject'
import type { Task, TaskPriority, TaskStatus } from '../types'

const COLUMNS: TaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Done']
const PRIORITIES: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical']

export function ProjectDetailPage() {
  const { projectId } = useParams<{ projectId: string }>()
  const { user } = useAuth()
  const { data: project, isLoading, isError } = useProject(projectId!)
  const { createTaskMutation, changeStatusMutation, assignMutation, addMemberMutation } = useProjectMutations(projectId!)

  const [title, setTitle] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [newMemberId, setNewMemberId] = useState('')

  function handleCreateTask(e: FormEvent) {
    e.preventDefault()
    if (!title.trim()) return
    createTaskMutation.mutate(
      { title, description: '', priority, dueDate: null },
      { onSuccess: () => setTitle('') },
    )
  }

  function handleAddMember(e: FormEvent) {
    e.preventDefault()
    if (!newMemberId.trim()) return
    addMemberMutation.mutate(newMemberId, { onSuccess: () => setNewMemberId('') })
  }

  if (isLoading) return <div className="page">Loading project…</div>
  if (isError || !project) return <div className="page error-text">Could not load this project.</div>

  const tasksByStatus = (status: TaskStatus): Task[] => project.tasks.filter((t) => t.status === status)
  const canManageMembers = user?.role === 'Admin' || user?.userId === project.ownerId

  return (
    <div className="page">
      <h1>{project.name}</h1>
      <p className="muted">{project.description}</p>

      <div className="card create-form">
        <h3>New task</h3>
        <form className="form-row" onSubmit={handleCreateTask}>
          <input placeholder="Task title" value={title} onChange={(e) => setTitle(e.target.value)} required />
          <select value={priority} onChange={(e) => setPriority(e.target.value as TaskPriority)}>
            {PRIORITIES.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
          <button type="submit" disabled={createTaskMutation.isPending}>
            {createTaskMutation.isPending ? 'Adding…' : 'Add task'}
          </button>
        </form>
        {createTaskMutation.error && (
          <p className="error-text" role="alert">
            {mutationErrorMessage(createTaskMutation.error, 'Could not add task. Please try again.')}
          </p>
        )}

        {canManageMembers && (
          <>
            <form className="form-row" onSubmit={handleAddMember}>
              <input
                placeholder="Add member by user id (from /api/auth/register response)"
                value={newMemberId}
                onChange={(e) => setNewMemberId(e.target.value)}
                disabled={addMemberMutation.isPending}
              />
              <button type="submit" disabled={addMemberMutation.isPending}>
                {addMemberMutation.isPending ? 'Adding…' : 'Add member'}
              </button>
            </form>
            {addMemberMutation.error && (
              <p className="error-text" role="alert">
                {mutationErrorMessage(addMemberMutation.error, 'Could not add the member. Please try again.')}
              </p>
            )}
          </>
        )}
      </div>

      <div className="board">
        {COLUMNS.map((status) => (
          <div key={status} className="board-column">
            <StatusBadge status={status} />
            {tasksByStatus(status).map((task) => (
              <TaskCard
                key={task.id}
                task={task}
                memberIds={project.memberIds}
                onStatusChange={(newStatus) => changeStatusMutation.mutate({ taskId: task.id, status: newStatus })}
                onAssign={(assigneeId) => assignMutation.mutate({ taskId: task.id, assigneeId })}
                isStatusUpdating={changeStatusMutation.isPending && changeStatusMutation.variables?.taskId === task.id}
                isAssignmentUpdating={assignMutation.isPending && assignMutation.variables?.taskId === task.id}
                statusError={changeStatusMutation.variables?.taskId === task.id && changeStatusMutation.error
                  ? mutationErrorMessage(changeStatusMutation.error, 'Could not update the task status. Please try again.')
                  : null}
                assignmentError={assignMutation.variables?.taskId === task.id && assignMutation.error
                  ? mutationErrorMessage(assignMutation.error, 'Could not assign the task. Please try again.')
                  : null}
              />
            ))}
            {tasksByStatus(status).length === 0 && <p className="muted empty-column">No tasks</p>}
          </div>
        ))}
      </div>
    </div>
  )
}
