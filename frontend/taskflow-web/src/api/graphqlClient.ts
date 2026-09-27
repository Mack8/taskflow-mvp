import { GraphQLClient, gql } from 'graphql-request'
import { GRAPHQL_URL } from './config'
import { getToken } from './httpClient'
import type { Project, ProjectDetail } from '../types'

// Reads go through GraphQL, writes go through REST (see ProjectsController /
// TasksController) — a deliberate CQRS-flavored split so the client can shape
// exactly the read it needs (e.g. project + tasks in one round trip) while
// commands keep the simplicity of plain HTTP verbs and status codes.
function client() {
  const token = getToken()
  return new GraphQLClient(GRAPHQL_URL, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
}

const PROJECTS_QUERY = gql`
  query Projects {
    projects {
      id
      name
      description
      ownerId
      memberCount
      taskCount
      createdAt
    }
  }
`

const PROJECT_QUERY = gql`
  query Project($projectId: UUID!) {
    project(projectId: $projectId) {
      id
      name
      description
      ownerId
      memberIds
      tasks {
        id
        projectId
        title
        description
        status
        priority
        assigneeId
        dueDate
        createdAt
        updatedAt
      }
    }
  }
`

export async function fetchProjects(): Promise<Project[]> {
  const data = await client().request<{ projects: Project[] }>(PROJECTS_QUERY)
  return data.projects
}

export async function fetchProject(projectId: string): Promise<ProjectDetail> {
  const data = await client().request<{ project: ProjectDetail }>(PROJECT_QUERY, { projectId })
  return data.project
}
