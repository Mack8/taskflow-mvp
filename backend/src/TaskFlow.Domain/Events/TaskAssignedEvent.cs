using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Events;

// Raised when a task gets (re)assigned. Published to the message broker after
// commit so the Notifications microservice can react independently — this is
// the seam that decouples the two services (event-driven architecture).
public sealed record TaskAssignedEvent(
    Guid TaskId,
    Guid ProjectId,
    string TaskTitle,
    Guid AssigneeId,
    Guid AssignedByUserId
) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
