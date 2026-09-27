using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskFlow.Notifications.Models;

namespace TaskFlow.Notifications.Services;

// The consuming half of the event-driven flow started in TaskFlow.Api's
// RabbitMqEventBus: binds a durable queue to just the two routing keys this
// service cares about, so it never sees traffic for events it doesn't handle.
// Independently deployable and independently scalable from the main API —
// this is the microservices boundary in the architecture.
public class NotificationEventConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly NotificationRepository _repository;
    private readonly ILogger<NotificationEventConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationEventConsumer(
        IOptions<RabbitMqOptions> options,
        NotificationRepository repository,
        ILogger<NotificationEventConsumer> logger)
    {
        _options = options.Value;
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel is null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                await HandleMessageAsync(ea.Body.ToArray(), stoppingToken);
                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message, sending to dead-letter via nack");
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(_options.Queue, autoAck: false, consumer);

        // Keep the background service alive for the lifetime of the host;
        // actual message handling happens on the consumer callback above.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                _connection = factory.CreateConnection("taskflow-notifications");
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
                _channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(_options.Queue, _options.Exchange, nameof(TaskAssignedEvent));
                _channel.QueueBind(_options.Queue, _options.Exchange, nameof(TaskStatusChangedEvent));

                _logger.LogInformation("Connected to RabbitMQ and bound queue {Queue}", _options.Queue);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ not ready (attempt {Attempt}/{Max}), retrying in 5s", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(byte[] body, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body);
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(json, JsonOptions)
            ?? throw new InvalidOperationException("Could not deserialize event envelope.");

        Notification? notification = envelope.EventType switch
        {
            nameof(TaskAssignedEvent) => MapTaskAssigned(envelope.Payload),
            nameof(TaskStatusChangedEvent) => MapTaskStatusChanged(envelope.Payload),
            _ => null
        };

        if (notification is null)
        {
            _logger.LogInformation("Ignoring unhandled event type {EventType}", envelope.EventType);
            return;
        }

        await _repository.InsertAsync(notification, cancellationToken);
        _logger.LogInformation("Stored notification for user {UserId} ({Type})", notification.UserId, notification.Type);
    }

    private static Notification MapTaskAssigned(JsonElement payload)
    {
        var evt = payload.Deserialize<TaskAssignedEvent>(JsonOptions)!;
        return new Notification
        {
            UserId = evt.AssigneeId,
            Type = nameof(TaskAssignedEvent),
            Message = $"You were assigned to task \"{evt.TaskTitle}\".",
            RelatedProjectId = evt.ProjectId,
            RelatedTaskId = evt.TaskId
        };
    }

    private static Notification? MapTaskStatusChanged(JsonElement payload)
    {
        var evt = payload.Deserialize<TaskStatusChangedEvent>(JsonOptions)!;
        if (evt.AssigneeId is null) return null;

        return new Notification
        {
            UserId = evt.AssigneeId.Value,
            Type = nameof(TaskStatusChangedEvent),
            Message = $"Task \"{evt.TaskTitle}\" moved from {evt.OldStatus} to {evt.NewStatus}.",
            RelatedProjectId = evt.ProjectId,
            RelatedTaskId = evt.TaskId
        };
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
