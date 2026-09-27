using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Events;
using Xunit;

namespace TaskFlow.Application.Tests.Domain;

public class TaskItemTests
{
    [Fact]
    public void ChangeStatus_ToNewStatus_RaisesTaskStatusChangedEvent()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write tests", "", TaskPriority.Medium, null);
        var changedBy = Guid.NewGuid();

        task.ChangeStatus(TaskState.InProgress, changedBy);

        task.Status.Should().Be(TaskState.InProgress);
        var domainEvent = task.DomainEvents.Should().ContainSingle().Subject;
        var statusChanged = domainEvent.Should().BeOfType<TaskStatusChangedEvent>().Subject;
        statusChanged.OldStatus.Should().Be(TaskState.Todo);
        statusChanged.NewStatus.Should().Be(TaskState.InProgress);
        statusChanged.ChangedByUserId.Should().Be(changedBy);
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_DoesNotRaiseEvent()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write tests", "", TaskPriority.Medium, null);

        task.ChangeStatus(TaskState.Todo, Guid.NewGuid());

        task.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AssignTo_RaisesTaskAssignedEvent_WithAssigneeId()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Write tests", "", TaskPriority.High, null);
        var assignee = Guid.NewGuid();
        var assigner = Guid.NewGuid();

        task.AssignTo(assignee, assigner);

        task.AssigneeId.Should().Be(assignee);
        var evt = task.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<TaskAssignedEvent>().Subject;
        evt.AssigneeId.Should().Be(assignee);
        evt.AssignedByUserId.Should().Be(assigner);
    }

    [Fact]
    public void Create_WithBlankTitle_Throws()
    {
        var act = () => TaskItem.Create(Guid.NewGuid(), "   ", "", TaskPriority.Low, null);

        act.Should().Throw<ArgumentException>();
    }
}
