import { PriorityBadge } from './PriorityBadge'
import type { Task, TaskStatus } from '../types'

const STATUSES: TaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Done']

interface Props {
  task: Task
  memberIds: string[]
  onStatusChange: (status: TaskStatus) => void
  onAssign: (assigneeId: string) => void
  isStatusUpdating?: boolean
  isAssignmentUpdating?: boolean
  statusError?: string | null
  assignmentError?: string | null
}

export function TaskCard({
  task,
  memberIds,
  onStatusChange,
  onAssign,
  isStatusUpdating = false,
  isAssignmentUpdating = false,
  statusError = null,
  assignmentError = null,
}: Props) {
  return (
    <div className="card task-card">
      <div className="task-card-header">
        <h4>{task.title}</h4>
        <PriorityBadge priority={task.priority} />
      </div>
      {task.description && <p className="muted">{task.description}</p>}

      <label className="field-inline">
        Status
        <select value={task.status} onChange={(e) => onStatusChange(e.target.value as TaskStatus)} disabled={isStatusUpdating}>
          {STATUSES.map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </label>
      {statusError && <p className="error-text" role="alert">{statusError}</p>}

      <label className="field-inline">
        Assignee
        <select
          value={task.assigneeId ?? ''}
          onChange={(e) => {
            if (e.target.value) onAssign(e.target.value)
          }}
          disabled={isAssignmentUpdating}
        >
          <option value="">Unassigned</option>
          {memberIds.map((id) => (
            <option key={id} value={id}>
              {id.slice(0, 8)}…
            </option>
          ))}
        </select>
      </label>
      {assignmentError && <p className="error-text" role="alert">{assignmentError}</p>}
    </div>
  )
}
