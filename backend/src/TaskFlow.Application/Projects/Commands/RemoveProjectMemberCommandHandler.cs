using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Projects.Commands;

public class RemoveProjectMemberCommandHandler : IRequestHandler<RemoveProjectMemberCommand>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RemoveProjectMemberCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        var isOwner = project.OwnerId == _currentUser.UserId;
        var isAdmin = _currentUser.Role == nameof(UserRole.Admin);
        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenAccessException("Only the project owner or an admin can remove members.");
        }

        // Project.RemoveMember throws InvalidOperationException for the owner;
        // ExceptionHandlingMiddleware maps that to 409 Conflict. A well-behaved
        // client (the admin UI) never offers a "remove" action on the owner row,
        // so this only fires against a hand-crafted request.
        project.RemoveMember(request.UserId);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
