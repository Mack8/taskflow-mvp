using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTimeOffset OccurredOn { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid id, string eventType, DateTimeOffset occurredOn, string payload)
    {
        Id = id;
        EventType = eventType;
        OccurredOn = occurredOn;
        Payload = payload;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public IntegrationEvent ToIntegrationEvent() => new(Id, EventType, OccurredOn, Payload);

    public void MarkPublished(DateTimeOffset publishedAt)
    {
        PublishedAt = publishedAt;
        NextAttemptAt = null;
        LastError = null;
    }

    public void MarkFailed(DateTimeOffset nextAttemptAt, string error)
    {
        AttemptCount++;
        NextAttemptAt = nextAttemptAt;
        LastError = error.Length <= 2000 ? error : error[..2000];
    }
}
