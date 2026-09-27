using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;

namespace TaskFlow.Infrastructure.Messaging;

// Publishes domain events to a durable topic exchange. Routing key is the CLR
// type name (e.g. "TaskAssignedEvent") so the Notifications microservice can
// bind a queue to just the events it cares about instead of consuming everything.
// Swapping this for an Azure Service Bus implementation behind IEventBus is the
// only change needed to run the same Application layer in Azure (see infra/bicep).
public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly Lazy<IConnection> _connection;

    // The Notifications microservice's event contracts declare enums (e.g.
    // TaskState) as strings, matching the string columns EF Core writes them
    // as. Without this converter, System.Text.Json's default is numeric,
    // which the consumer's string-typed contract then fails to deserialize.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public RabbitMqEventBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
        _connection = new Lazy<IConnection>(CreateConnection);
    }

    private IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = false
        };
        return factory.CreateConnection("taskflow-api");
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = _connection.Value.CreateModel();
            channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);

            var routingKey = domainEvent.GetType().Name;

            // domainEvent.GetType() (the RUNTIME type, e.g. TaskAssignedEvent) is
            // passed explicitly here. Letting Payload take its compile-time type
            // (IDomainEvent) instead would make System.Text.Json serialize only
            // that interface's members — just OccurredOn — silently dropping every
            // other field (TaskId, AssigneeId, ...) with no error.
            var payload = JsonSerializer.SerializeToElement(domainEvent, domainEvent.GetType(), JsonOptions);
            var envelope = new
            {
                EventType = routingKey,
                OccurredOn = domainEvent.OccurredOn,
                Payload = payload
            };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            channel.BasicPublish(_options.Exchange, routingKey, properties, body);
            _logger.LogInformation("Published {EventType} to exchange {Exchange}", routingKey, _options.Exchange);
        }
        catch (Exception ex)
        {
            // A broker outage should never take down a write request; the event
            // is logged and dropped. A production system would add an outbox
            // table here instead of failing open.
            _logger.LogError(ex, "Failed to publish domain event {EventType}", domainEvent.GetType().Name);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
