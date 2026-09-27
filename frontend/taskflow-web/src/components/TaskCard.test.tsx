import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { TaskCard } from './TaskCard'
import type { Task } from '../types'

const task: Task = {
  id: 't1',
  projectId: 'p1',
  title: 'Write the README',
  description: 'Cover architecture decisions',
  status: 'Todo',
  priority: 'High',
  assigneeId: null,
  dueDate: null,
  createdAt: new Date().toISOString(),
  updatedAt: null,
}

describe('TaskCard', () => {
  it('calls onStatusChange when a new status is selected', () => {
    const onStatusChange = vi.fn()
    render(<TaskCard task={task} memberIds={['user-1']} onStatusChange={onStatusChange} onAssign={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'InProgress' } })

    expect(onStatusChange).toHaveBeenCalledWith('InProgress')
  })

  it('calls onAssign when a member is picked from the assignee select', () => {
    const onAssign = vi.fn()
    render(<TaskCard task={task} memberIds={['user-1']} onStatusChange={vi.fn()} onAssign={onAssign} />)

    fireEvent.change(screen.getByLabelText('Assignee'), { target: { value: 'user-1' } })

    expect(onAssign).toHaveBeenCalledWith('user-1')
  })
})
