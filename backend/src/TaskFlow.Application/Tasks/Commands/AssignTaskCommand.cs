using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Tasks.Commands;

public record AssignTaskCommand(Guid TaskId, Guid AssigneeId) : IRequest<TaskDto>;
