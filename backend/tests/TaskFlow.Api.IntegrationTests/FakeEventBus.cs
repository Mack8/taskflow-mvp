using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;

namespace TaskFlow.Api.IntegrationTests;

// Swapped in for RabbitMqEventBus in tests: asserting HTTP behavior shouldn't
// require a running broker. Published events are captured so a test can still
// assert "an event was raised" without touching RabbitMQ.
public class FakeEventBus : IEventBus
{
    public List<IntegrationEvent> PublishedEvents { get; } = new();

    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(integrationEvent);
        return Task.CompletedTask;
    }
}
