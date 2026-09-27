using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Events;

public sealed record TaskStatusChangedEvent(
    Guid TaskId,
    Guid ProjectId,
    string TaskTitle,
    Guid? AssigneeId,
    TaskState OldStatus,
    TaskState NewStatus,
    Guid ChangedByUserId
) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
