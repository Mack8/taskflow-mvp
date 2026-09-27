using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Commands;

public record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string Description,
    TaskPriority Priority,
    DateTimeOffset? DueDate) : IRequest<TaskDto>;
