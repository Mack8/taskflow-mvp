using FluentAssertions;
using Moq;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.Application.Tests.Projects;

public class RemoveProjectMemberCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private RemoveProjectMemberCommandHandler CreateHandler() =>
        new(_projects.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Admin_CanRemoveMemberFromAnyProject()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = Project.Create("Demo", "", ownerId);
        project.AddMember(memberId);
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(c => c.Role).Returns("Admin");

        var handler = CreateHandler();
        await handler.Handle(new RemoveProjectMemberCommand(project.Id, memberId), CancellationToken.None);

        project.HasMember(memberId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PlainMember_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = Project.Create("Demo", "", ownerId);
        project.AddMember(memberId);
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _currentUser.Setup(c => c.UserId).Returns(memberId);
        _currentUser.Setup(c => c.Role).Returns("Member");

        var handler = CreateHandler();
        var act = () => handler.Handle(new RemoveProjectMemberCommand(project.Id, memberId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
