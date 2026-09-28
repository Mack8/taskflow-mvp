using TaskFlow.Domain.Common;

namespace TaskFlow.Application.Common.Interfaces;

// Abstraction over the message broker. The outbox publisher invokes it with a
// durable delivery contract after the domain transaction has committed.
public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
