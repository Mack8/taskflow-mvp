import type { TaskStatus } from '../types'

const LABELS: Record<TaskStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  InReview: 'In review',
  Done: 'Done',
}

export function StatusBadge({ status }: { status: TaskStatus }) {
  return <span className={`badge badge-status-${status.toLowerCase()}`}>{LABELS[status]}</span>
}
