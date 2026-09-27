import { useState, type FormEvent } from 'react'
import { ProjectCard } from '../components/ProjectCard'
import { useCreateProject, useProjects } from '../hooks/useProjects'

export function ProjectsPage() {
  const { data: projects, isLoading, isError } = useProjects()
  const createProject = useCreateProject()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) return
    createProject.mutate(
      { name, description },
      {
        onSuccess: () => {
          setName('')
          setDescription('')
        },
      },
    )
  }

  return (
    <div className="page">
      <h1>Your projects</h1>

      <form className="card create-form" onSubmit={handleSubmit}>
        <h3>New project</h3>
        <div className="form-row">
          <input placeholder="Project name" value={name} onChange={(e) => setName(e.target.value)} required />
          <input placeholder="Description (optional)" value={description} onChange={(e) => setDescription(e.target.value)} />
          <button type="submit" disabled={createProject.isPending}>
            {createProject.isPending ? 'Creating…' : 'Create'}
          </button>
        </div>
      </form>

      {isLoading && <p className="muted">Loading projects…</p>}
      {isError && <p className="error-text">Could not load projects.</p>}

      <div className="project-grid">
        {projects?.map((project) => (
          <ProjectCard key={project.id} project={project} />
        ))}
        {projects?.length === 0 && <p className="muted">No projects yet — create your first one above.</p>}
      </div>
    </div>
  )
}
