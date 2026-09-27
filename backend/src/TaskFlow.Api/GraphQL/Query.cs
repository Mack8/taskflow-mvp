using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Projects.Queries;
using TaskFlow.Application.Tasks.Queries;

namespace TaskFlow.Api.GraphQL;

// Read side of CQRS exposed as GraphQL: each resolver just forwards to the
// same MediatR queries the REST controllers could call, so callers pick
// REST when they know exactly which shape they want, or GraphQL when they
// want to compose projects + tasks in one round trip (see ProjectDetail below).
public class Query
{
    public async Task<List<ProjectDto>> GetProjects([Service] ISender sender, CancellationToken cancellationToken) =>
        await sender.Send(new GetProjectsQuery(), cancellationToken);

    public async Task<ProjectDetailDto> GetProject([Service] ISender sender, Guid projectId, CancellationToken cancellationToken) =>
        await sender.Send(new GetProjectByIdQuery(projectId), cancellationToken);

    public async Task<List<TaskDto>> GetTasksByProject([Service] ISender sender, Guid projectId, CancellationToken cancellationToken) =>
        await sender.Send(new GetTasksByProjectQuery(projectId), cancellationToken);
}
