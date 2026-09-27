using MediatR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Projects.Commands;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, request.Description, _currentUser.UserId);

        await _projects.AddAsync(project, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new ProjectDto(project.Id, project.Name, project.Description, project.OwnerId, project.Members.Count, project.Tasks.Count, project.CreatedAt);
    }
}
