using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects.Queries;

public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, List<ProjectDto>>
{
    private readonly IProjectRepository _projects;
    private readonly ICurrentUserService _currentUser;

    public GetAllProjectsQueryHandler(IProjectRepository projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public async Task<List<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != nameof(UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only an admin can list every project.");
        }

        var projects = await _projects.GetAllAsync(cancellationToken);

        return projects
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.OwnerId, p.Members.Count, p.Tasks.Count, p.CreatedAt))
            .ToList();
    }
}
