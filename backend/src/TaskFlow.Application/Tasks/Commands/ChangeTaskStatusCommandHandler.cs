using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Tasks.Commands;

public class ChangeTaskStatusCommandHandler : IRequestHandler<ChangeTaskStatusCommand, TaskDto>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ChangeTaskStatusCommandHandler(
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

    public async Task<TaskDto> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("TaskItem", request.TaskId);

        var project = await _projects.GetByIdAsync(task.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), task.ProjectId);

        if (!project.HasMember(_currentUser.UserId))
        {
            throw new ForbiddenAccessException("You are not a member of this project.");
        }

        task.ChangeStatus(request.NewStatus, _currentUser.UserId);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new TaskDto(task.Id, task.ProjectId, task.Title, task.Description, task.Status.ToString(),
            task.Priority.ToString(), task.AssigneeId, task.DueDate, task.CreatedAt, task.UpdatedAt);
    }
}
