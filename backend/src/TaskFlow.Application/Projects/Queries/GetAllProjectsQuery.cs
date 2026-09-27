using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Projects.Queries;

// Admin-only counterpart to GetProjectsQuery: every project in the system,
// not just ones the caller is a member of, so an admin can pick any project
// to assign a user to.
public record GetAllProjectsQuery : IRequest<List<ProjectDto>>;
