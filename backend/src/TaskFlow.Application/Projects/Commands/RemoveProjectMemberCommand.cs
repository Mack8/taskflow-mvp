using MediatR;

namespace TaskFlow.Application.Projects.Commands;

public record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : IRequest;
