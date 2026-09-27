using TaskFlow.Domain.Common;

namespace TaskFlow.Application.Common.Interfaces;

// Abstraction over the message broker. Infrastructure implements this with
// RabbitMQ.Client locally; in Azure the same interface would be backed by
// Azure Service Bus (see infra/bicep) without Application code changing.
public interface IEventBus
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
