using MediatR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Projects.Queries;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, List<ProjectDto>>
{
    private readonly IProjectRepository _projects;
    private readonly ICurrentUserService _currentUser;

    public GetProjectsQueryHandler(IProjectRepository projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public async Task<List<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projects.GetForUserAsync(_currentUser.UserId, cancellationToken);

        return projects
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.OwnerId, p.Members.Count, p.Tasks.Count, p.CreatedAt))
            .ToList();
    }
}
