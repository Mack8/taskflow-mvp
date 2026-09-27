using FluentAssertions;
using Moq;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.Application.Tests.Projects;

public class AddProjectMemberCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private AddProjectMemberCommandHandler CreateHandler() =>
        new(_projects.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_ProjectOwner_AddsMember()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Demo", "", ownerId);
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _currentUser.Setup(c => c.Role).Returns("Member");

        var handler = CreateHandler();
        var newMemberId = Guid.NewGuid();
        await handler.Handle(new AddProjectMemberCommand(project.Id, newMemberId), CancellationToken.None);

        project.HasMember(newMemberId).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Admin_CanAddMemberToAnyProject_EvenWithoutOwningIt()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var project = Project.Create("Demo", "", ownerId);
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _currentUser.Setup(c => c.UserId).Returns(adminId);
        _currentUser.Setup(c => c.Role).Returns("Admin");

        var handler = CreateHandler();
        var newMemberId = Guid.NewGuid();
        await handler.Handle(new AddProjectMemberCommand(project.Id, newMemberId), CancellationToken.None);

        project.HasMember(newMemberId).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PlainMember_NotOwnerNotAdmin_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Demo", "", ownerId);
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(c => c.Role).Returns("Member");

        var handler = CreateHandler();
        var act = () => handler.Handle(new AddProjectMemberCommand(project.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
