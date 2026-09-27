using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Tasks.Commands;

// Publishing the domain event happens inside IUnitOfWork.CommitAsync (via the
// EF Core DbContext override in Infrastructure), *after* SaveChanges succeeds —
// so the Notifications microservice never hears about an assignment that
// didn't actually persist.
public class AssignTaskCommandHandler : IRequestHandler<AssignTaskCommand, TaskDto>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AssignTaskCommandHandler(
        ITaskRepository tasks,
        IProjectRepository projects,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _tasks = tasks;
        _projects = projects;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<TaskDto> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("TaskItem", request.TaskId);

        var project = await _projects.GetByIdAsync(task.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), task.ProjectId);

        if (!project.HasMember(_currentUser.UserId))
        {
            throw new ForbiddenAccessException("You are not a member of this project.");
        }

        if (!project.HasMember(request.AssigneeId))
        {
            throw new ConflictException("The assignee must be a member of the project.");
        }

        task.AssignTo(request.AssigneeId, _currentUser.UserId);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new TaskDto(task.Id, task.ProjectId, task.Title, task.Description, task.Status.ToString(),
            task.Priority.ToString(), task.AssigneeId, task.DueDate, task.CreatedAt, task.UpdatedAt);
    }
}
