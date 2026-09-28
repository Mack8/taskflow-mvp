namespace TaskFlow.Domain.Common;

// Persisted delivery contract. EventId stays the same on every retry so a
// consumer can later make delivery idempotent without changing the producer.
public sealed record IntegrationEvent(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredOn,
    string Payload);
