namespace TaskFlow.Domain.Common;

// Marker interface for domain events raised by aggregates. Infrastructure
// (MediatR + RabbitMQ publisher) picks these up after SaveChanges succeeds,
// so a message is never published for a write that didn't commit.
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
