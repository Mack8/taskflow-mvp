using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateTaskCommandHandler(
        IProjectRepository projects,
        ITaskRepository tasks,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _projects = projects;
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        if (!project.HasMember(_currentUser.UserId))
        {
            throw new ForbiddenAccessException("You are not a member of this project.");
        }

        var task = TaskItem.Create(request.ProjectId, request.Title, request.Description, request.Priority, request.DueDate);

        await _tasks.AddAsync(task, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new TaskDto(task.Id, task.ProjectId, task.Title, task.Description, task.Status.ToString(),
            task.Priority.ToString(), task.AssigneeId, task.DueDate, task.CreatedAt, task.UpdatedAt);
    }
}
