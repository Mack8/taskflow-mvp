import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ProjectDetailPage } from './ProjectDetailPage'

const mocks = vi.hoisted(() => ({
  useAuth: vi.fn(),
  useProject: vi.fn(),
  useProjectMutations: vi.fn(),
}))

vi.mock('react-router-dom', () => ({ useParams: () => ({ projectId: 'project-1' }) }))
vi.mock('../auth/AuthContext', () => ({ useAuth: mocks.useAuth }))
vi.mock('../hooks/useProject', () => ({
  useProject: mocks.useProject,
  useProjectMutations: mocks.useProjectMutations,
}))

const project = {
  id: 'project-1',
  name: 'Roadmap',
  description: '',
  ownerId: 'owner-1',
  memberIds: ['owner-1', 'member-1'],
  tasks: [{
    id: 'task-1', projectId: 'project-1', title: 'Plan launch', description: '', status: 'Todo', priority: 'Medium',
    assigneeId: null, dueDate: null, createdAt: '2026-01-01T00:00:00Z', updatedAt: null,
  }],
}

function mutation(overrides: Record<string, unknown> = {}) {
  return { mutate: vi.fn(), isPending: false, error: null, variables: undefined, ...overrides }
}

describe('ProjectDetailPage', () => {
  beforeEach(() => {
    mocks.useProject.mockReturnValue({ data: project, isLoading: false, isError: false })
    mocks.useProjectMutations.mockReturnValue({
      createTaskMutation: mutation(),
      changeStatusMutation: mutation(),
      assignMutation: mutation(),
      addMemberMutation: mutation(),
    })
  })

  it('hides member management from a regular project member', () => {
    mocks.useAuth.mockReturnValue({ user: { userId: 'member-1', role: 'Member' } })

    render(<ProjectDetailPage />)

    expect(screen.queryByPlaceholderText(/Add member by user id/i)).not.toBeInTheDocument()
  })

  it('keeps task form data and shows an API validation error', () => {
    mocks.useAuth.mockReturnValue({ user: { userId: 'owner-1', role: 'Member' } })
    mocks.useProjectMutations.mockReturnValue({
      createTaskMutation: mutation({ error: {
        isAxiosError: true,
        response: { status: 400, data: { errors: { Title: ['Title is required.'] } } },
      } }),
      changeStatusMutation: mutation({ isPending: true, variables: { taskId: 'task-1' } }),
      assignMutation: mutation(),
      addMemberMutation: mutation(),
    })

    render(<ProjectDetailPage />)
    fireEvent.change(screen.getByPlaceholderText('Task title'), { target: { value: 'Prepare launch' } })

    expect(screen.getByPlaceholderText('Task title')).toHaveValue('Prepare launch')
    expect(screen.getByRole('alert')).toHaveTextContent('Title is required.')
    expect(screen.getByLabelText('Status')).toBeDisabled()
    expect(screen.getByLabelText('Assignee')).toBeEnabled()
    expect(screen.getByPlaceholderText(/Add member by user id/i)).toBeInTheDocument()
  })
})
