using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Projects.Queries;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IProjectRepository _projects;
    private readonly ICurrentUserService _currentUser;

    public GetProjectByIdQueryHandler(IProjectRepository projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetWithTasksAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        if (!project.HasMember(_currentUser.UserId))
        {
            throw new ForbiddenAccessException("You are not a member of this project.");
        }

        var tasks = project.Tasks.Select(t => new TaskDto(
            t.Id, t.ProjectId, t.Title, t.Description, t.Status.ToString(), t.Priority.ToString(),
            t.AssigneeId, t.DueDate, t.CreatedAt, t.UpdatedAt)).ToList();

        return new ProjectDetailDto(
            project.Id, project.Name, project.Description, project.OwnerId,
            project.Members.Select(m => m.UserId).ToList(), tasks);
    }
}
