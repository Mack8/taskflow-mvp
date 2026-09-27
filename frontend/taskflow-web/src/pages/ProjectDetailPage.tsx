import { useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { StatusBadge } from '../components/StatusBadge'
import { TaskCard } from '../components/TaskCard'
import { useProject, useProjectMutations } from '../hooks/useProject'
import type { Task, TaskPriority, TaskStatus } from '../types'

const COLUMNS: TaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Done']
const PRIORITIES: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical']

export function ProjectDetailPage() {
  const { projectId } = useParams<{ projectId: string }>()
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

        <form className="form-row" onSubmit={handleAddMember}>
          <input
            placeholder="Add member by user id (from /api/auth/register response)"
            value={newMemberId}
            onChange={(e) => setNewMemberId(e.target.value)}
          />
          <button type="submit" disabled={addMemberMutation.isPending}>
            Add member
          </button>
        </form>
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
              />
            ))}
            {tasksByStatus(status).length === 0 && <p className="muted empty-column">No tasks</p>}
          </div>
        ))}
      </div>
    </div>
  )
}
