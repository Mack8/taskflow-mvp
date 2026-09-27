using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Tasks.Queries;

public class GetTasksByProjectQueryHandler : IRequestHandler<GetTasksByProjectQuery, List<TaskDto>>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly ICurrentUserService _currentUser;

    public GetTasksByProjectQueryHandler(ITaskRepository tasks, IProjectRepository projects, ICurrentUserService currentUser)
    {
        _tasks = tasks;
        _projects = projects;
        _currentUser = currentUser;
    }

    public async Task<List<TaskDto>> Handle(GetTasksByProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        if (!project.HasMember(_currentUser.UserId))
        {
            throw new ForbiddenAccessException("You are not a member of this project.");
        }

        var tasks = await _tasks.GetByProjectAsync(request.ProjectId, cancellationToken);

        return tasks.Select(t => new TaskDto(t.Id, t.ProjectId, t.Title, t.Description, t.Status.ToString(),
            t.Priority.ToString(), t.AssigneeId, t.DueDate, t.CreatedAt, t.UpdatedAt)).ToList();
    }
}
