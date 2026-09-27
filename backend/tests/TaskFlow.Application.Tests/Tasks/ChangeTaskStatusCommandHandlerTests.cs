using FluentAssertions;
using Moq;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.Application.Tests.Tasks;

public class ChangeTaskStatusCommandHandlerTests
{
    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private ChangeTaskStatusCommandHandler CreateHandler() =>
        new(_tasks.Object, _projects.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_MemberOfProject_UpdatesStatus()
    {
        var userId = Guid.NewGuid();
        var project = Project.Create("Demo", "", userId);
        var task = TaskItem.Create(project.Id, "Ship it", "", TaskPriority.High, null);

        _tasks.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = CreateHandler();
        var result = await handler.Handle(new ChangeTaskStatusCommand(task.Id, TaskState.Done), CancellationToken.None);

        result.Status.Should().Be(nameof(TaskState.Done));
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonMember_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Demo", "", ownerId);
        var task = TaskItem.Create(project.Id, "Ship it", "", TaskPriority.High, null);

        _tasks.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projects.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var handler = CreateHandler();
        var act = () => handler.Handle(new ChangeTaskStatusCommand(task.Id, TaskState.Done), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownTask_ThrowsNotFound()
    {
        _tasks.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new ChangeTaskStatusCommand(Guid.NewGuid(), TaskState.Done), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
