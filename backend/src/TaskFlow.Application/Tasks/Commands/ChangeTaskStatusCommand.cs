using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Commands;

public record ChangeTaskStatusCommand(Guid TaskId, TaskState NewStatus) : IRequest<TaskDto>;
