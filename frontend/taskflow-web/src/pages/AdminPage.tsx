import { useState, type FormEvent } from 'react'
import { useAdminProjectMemberMutations, useAllProjects, useCreateUser, useUsers } from '../hooks/useAdmin'
import { useProject } from '../hooks/useProject'
import type { UserRole } from '../types'

export function AdminPage() {
  return (
    <div className="page">
      <h1>Admin</h1>
      <p className="muted">Create accounts, and assign or remove any user on any project — regardless of ownership.</p>

      <CreateUserSection />
      <UsersSection />
      <ProjectsSection />
    </div>
  )
}

function CreateUserSection() {
  const createUser = useCreateUser()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<UserRole>('Member')
  const [feedback, setFeedback] = useState<string | null>(null)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setFeedback(null)
    createUser.mutate(
      { name, email, password, role },
      {
        onSuccess: () => {
          setName('')
          setEmail('')
          setPassword('')
          setRole('Member')
          setFeedback(`Created ${email}.`)
        },
        onError: () => setFeedback('Could not create user — email may already be in use.'),
      },
    )
  }

  return (
    <section className="card create-form">
      <h3>Create user</h3>
      <form className="form-row" onSubmit={handleSubmit}>
        <input placeholder="Name" value={name} onChange={(e) => setName(e.target.value)} required />
        <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        <input
          type="password"
          placeholder="Password (min 8 chars)"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          minLength={8}
          required
        />
        <select value={role} onChange={(e) => setRole(e.target.value as UserRole)}>
          <option value="Member">Member</option>
          <option value="Admin">Admin</option>
        </select>
        <button type="submit" disabled={createUser.isPending}>
          {createUser.isPending ? 'Creating…' : 'Create'}
        </button>
      </form>
      {feedback && <p className="muted">{feedback}</p>}
    </section>
  )
}

function UsersSection() {
  const { data: users, isLoading } = useUsers()

  return (
    <section className="card">
      <h3>Users</h3>
      {isLoading && <p className="muted">Loading…</p>}
      <table className="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
          </tr>
        </thead>
        <tbody>
          {users?.map((u) => (
            <tr key={u.id}>
              <td>{u.name}</td>
              <td>{u.email}</td>
              <td>
                <span className={`badge ${u.role === 'Admin' ? 'badge-priority-critical' : 'badge-status-todo'}`}>{u.role}</span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}

function ProjectsSection() {
  const { data: projects, isLoading } = useAllProjects()
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(null)

  return (
    <section className="card">
      <h3>All projects</h3>
      {isLoading && <p className="muted">Loading…</p>}
      <table className="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Members</th>
            <th>Tasks</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {projects?.map((p) => (
            <tr key={p.id}>
              <td>{p.name}</td>
              <td>{p.memberCount}</td>
              <td>{p.taskCount}</td>
              <td>
                <button className="secondary" onClick={() => setSelectedProjectId(p.id)}>
                  Manage members
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {selectedProjectId && <ProjectMembersPanel projectId={selectedProjectId} onClose={() => setSelectedProjectId(null)} />}
    </section>
  )
}

function ProjectMembersPanel({ projectId, onClose }: { projectId: string; onClose: () => void }) {
  const { data: project, isLoading } = useProject(projectId)
  const { data: users } = useUsers()
  const { assignMutation, removeMutation } = useAdminProjectMemberMutations()
  const [newMemberId, setNewMemberId] = useState('')

  if (isLoading || !project) return <p className="muted">Loading project…</p>

  const nonMembers = users?.filter((u) => !project.memberIds.includes(u.id)) ?? []

  function handleAssign(e: FormEvent) {
    e.preventDefault()
    if (!newMemberId) return
    assignMutation.mutate({ projectId, userId: newMemberId }, { onSuccess: () => setNewMemberId('') })
  }

  return (
    <div className="admin-members-panel">
      <div className="task-card-header">
        <h4>{project.name} — members</h4>
        <button className="secondary" onClick={onClose}>
          Close
        </button>
      </div>

      <ul>
        {project.memberIds.map((memberId) => {
          const isOwner = memberId === project.ownerId
          const member = users?.find((u) => u.id === memberId)
          return (
            <li key={memberId}>
              <span>
                {member?.name ?? memberId} {isOwner && <em className="muted">(owner)</em>}
              </span>
              {!isOwner && (
                <button
                  className="link-button"
                  onClick={() => removeMutation.mutate({ projectId, userId: memberId })}
                  disabled={removeMutation.isPending}
                >
                  Remove
                </button>
              )}
            </li>
          )
        })}
      </ul>

      <form className="form-row" onSubmit={handleAssign}>
        <select value={newMemberId} onChange={(e) => setNewMemberId(e.target.value)}>
          <option value="">Select a user to add…</option>
          {nonMembers.map((u) => (
            <option key={u.id} value={u.id}>
              {u.name} ({u.email})
            </option>
          ))}
        </select>
        <button type="submit" disabled={!newMemberId || assignMutation.isPending}>
          Add to project
        </button>
      </form>
    </div>
  )
}
