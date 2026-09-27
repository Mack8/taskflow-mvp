using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Tasks.Queries;

public record GetTasksByProjectQuery(Guid ProjectId) : IRequest<List<TaskDto>>;
