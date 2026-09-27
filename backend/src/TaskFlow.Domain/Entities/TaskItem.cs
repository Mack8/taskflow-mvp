using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Events;

namespace TaskFlow.Domain.Entities;

public class TaskItem : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Project? Project { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskState Status { get; private set; } = TaskState.Todo;
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
    public Guid? AssigneeId { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    private TaskItem() { }

    public static TaskItem Create(Guid projectId, string title, string description, TaskPriority priority, DateTimeOffset? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));

        return new TaskItem
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Priority = priority,
            DueDate = dueDate
        };
    }

    public void AssignTo(Guid assigneeId, Guid assignedByUserId)
    {
        AssigneeId = assigneeId;
        Touch();
        AddDomainEvent(new TaskAssignedEvent(Id, ProjectId, Title, assigneeId, assignedByUserId));
    }

    public void ChangeStatus(TaskState newStatus, Guid changedByUserId)
    {
        if (newStatus == Status) return;

        var oldStatus = Status;
        Status = newStatus;
        Touch();
        AddDomainEvent(new TaskStatusChangedEvent(Id, ProjectId, Title, AssigneeId, oldStatus, newStatus, changedByUserId));
    }
}
