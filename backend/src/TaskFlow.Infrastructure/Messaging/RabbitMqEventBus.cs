using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;

namespace TaskFlow.Infrastructure.Messaging;

// Publishes persisted integration events to a durable topic exchange. The type
// remains the routing key (e.g. "TaskAssignedEvent") so Notifications binds
// only the messages it needs. Publisher confirms decide whether outbox rows can
// be marked delivered.
public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly object _connectionLock = new();
    private IConnection? _connection;

    public RabbitMqEventBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
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

    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var channel = GetConnection().CreateModel();
        channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
        channel.ConfirmSelect();

        using var payloadDocument = JsonDocument.Parse(integrationEvent.Payload);
        var envelope = new
        {
            EventId = integrationEvent.EventId,
            integrationEvent.EventType,
            integrationEvent.OccurredOn,
            Payload = payloadDocument.RootElement.Clone()
        };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = integrationEvent.EventId.ToString();

        channel.BasicPublish(_options.Exchange, integrationEvent.EventType, properties, body);
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        _logger.LogInformation("Published outbox event {EventId} ({EventType}) to exchange {Exchange}",
            integrationEvent.EventId, integrationEvent.EventType, _options.Exchange);

        return Task.CompletedTask;
    }

    private IConnection GetConnection()
    {
        lock (_connectionLock)
        {
            if (_connection?.IsOpen == true) return _connection;
            _connection?.Dispose();
            _connection = CreateConnection();
            return _connection;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
