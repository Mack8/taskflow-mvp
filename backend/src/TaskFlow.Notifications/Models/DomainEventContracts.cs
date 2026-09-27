namespace TaskFlow.Notifications.Models;

// These mirror the event shapes published by TaskFlow.Api (see
// TaskFlow.Domain.Events + TaskFlow.Infrastructure.Messaging.RabbitMqEventBus)
// but are declared independently here rather than shared via a common library.
// That duplication is intentional: it is the price of true microservice
// decoupling — this service can evolve its own contract copy without forcing
// a synchronized deploy with the Api service, at the cost of the two staying
// in sync by convention instead of by compiler.
public record EventEnvelope(string EventType, DateTimeOffset OccurredOn, System.Text.Json.JsonElement Payload);

public record TaskAssignedEvent(Guid TaskId, Guid ProjectId, string TaskTitle, Guid AssigneeId, Guid AssignedByUserId);

public record TaskStatusChangedEvent(Guid TaskId, Guid ProjectId, string TaskTitle, Guid? AssigneeId, string OldStatus, string NewStatus, Guid ChangedByUserId);
