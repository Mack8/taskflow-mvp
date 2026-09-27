using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Projects.Commands;

public record CreateProjectCommand(string Name, string Description) : IRequest<ProjectDto>;
