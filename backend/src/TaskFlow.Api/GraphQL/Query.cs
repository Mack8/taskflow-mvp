using HotChocolate.Authorization;
using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Projects.Queries;
using TaskFlow.Application.Tasks.Queries;
using TaskFlow.Application.Users.Queries;

namespace TaskFlow.Api.GraphQL;

// Read side of CQRS exposed as GraphQL: each resolver just forwards to the
// same MediatR queries the REST controllers could call, so callers pick
// REST when they know exactly which shape they want, or GraphQL when they
// want to compose projects + tasks in one round trip (see ProjectDetail below).
//
// GetUsers/GetAllProjects carry [Authorize(Roles = ...)] directly on the
// resolver — HotChocolate enforces it per-field, independently of the
// [Authorize(Roles = "Admin")] on AdminController, since a GraphQL query has
// no controller action for that attribute to sit on.
public class Query
{
    public async Task<List<ProjectDto>> GetProjects([Service] ISender sender, CancellationToken cancellationToken) =>
        await sender.Send(new GetProjectsQuery(), cancellationToken);

    public async Task<ProjectDetailDto> GetProject([Service] ISender sender, Guid projectId, CancellationToken cancellationToken) =>
        await sender.Send(new GetProjectByIdQuery(projectId), cancellationToken);

    public async Task<List<TaskDto>> GetTasksByProject([Service] ISender sender, Guid projectId, CancellationToken cancellationToken) =>
        await sender.Send(new GetTasksByProjectQuery(projectId), cancellationToken);

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<List<UserSummaryDto>> GetUsers([Service] ISender sender, CancellationToken cancellationToken) =>
        await sender.Send(new GetUsersQuery(), cancellationToken);

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<List<ProjectDto>> GetAllProjects([Service] ISender sender, CancellationToken cancellationToken) =>
        await sender.Send(new GetAllProjectsQuery(), cancellationToken);
}
