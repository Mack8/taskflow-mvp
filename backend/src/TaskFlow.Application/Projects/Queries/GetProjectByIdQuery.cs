using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Projects.Queries;

public record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDetailDto>;
