import { Link } from 'react-router-dom'
import type { Project } from '../types'

export function ProjectCard({ project }: { project: Project }) {
  return (
    <Link to={`/projects/${project.id}`} className="card project-card">
      <h3>{project.name}</h3>
      <p className="muted">{project.description || 'No description'}</p>
      <div className="project-card-meta">
        <span>{project.memberCount} member{project.memberCount === 1 ? '' : 's'}</span>
        <span>{project.taskCount} task{project.taskCount === 1 ? '' : 's'}</span>
      </div>
    </Link>
  )
}
