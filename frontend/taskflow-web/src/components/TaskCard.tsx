import { useState } from 'react'
import { PriorityBadge } from './PriorityBadge'
import type { Task, TaskStatus } from '../types'

const STATUSES: TaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Done']

interface Props {
  task: Task
  memberIds: string[]
  onStatusChange: (status: TaskStatus) => void
  onAssign: (assigneeId: string) => void
}

export function TaskCard({ task, memberIds, onStatusChange, onAssign }: Props) {
  const [assignee, setAssignee] = useState(task.assigneeId ?? '')

  return (
    <div className="card task-card">
      <div className="task-card-header">
        <h4>{task.title}</h4>
        <PriorityBadge priority={task.priority} />
      </div>
      {task.description && <p className="muted">{task.description}</p>}

      <label className="field-inline">
        Status
        <select value={task.status} onChange={(e) => onStatusChange(e.target.value as TaskStatus)}>
          {STATUSES.map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </label>

      <label className="field-inline">
        Assignee
        <select
          value={assignee}
          onChange={(e) => {
            setAssignee(e.target.value)
            if (e.target.value) onAssign(e.target.value)
          }}
        >
          <option value="">Unassigned</option>
          {memberIds.map((id) => (
            <option key={id} value={id}>
              {id.slice(0, 8)}…
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}
