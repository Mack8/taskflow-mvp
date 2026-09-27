using MediatR;

namespace TaskFlow.Application.Projects.Commands;

public record AddProjectMemberCommand(Guid ProjectId, Guid UserId) : IRequest;
