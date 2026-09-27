using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Projects.Commands;

public class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddProjectMemberCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        if (project.OwnerId != _currentUser.UserId)
        {
            throw new ForbiddenAccessException("Only the project owner can add members.");
        }

        project.AddMember(request.UserId);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
