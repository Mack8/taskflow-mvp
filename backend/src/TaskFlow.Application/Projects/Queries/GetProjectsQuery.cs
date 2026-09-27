using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Projects.Queries;

// Query side of CQRS: read-only, no validators/side effects, mapped 1:1 to the
// GraphQL schema in TaskFlow.Api/GraphQL — commands stay on REST, reads are
// exposed through GraphQL so clients can shape the response they need.
public record GetProjectsQuery : IRequest<List<ProjectDto>>;
