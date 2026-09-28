using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Messaging;

public class OutboxProcessor
{
    private readonly ApplicationDbContext _db;
    private readonly IEventBus _eventBus;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        ApplicationDbContext db,
        IEventBus eventBus,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _db = db;
        _eventBus = eventBus;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var batchSize = Math.Clamp(_options.BatchSize, 1, 100);
        var messages = await _db.OutboxMessages
            .Where(message => message.PublishedAt == null &&
                (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await _eventBus.PublishAsync(message.ToIntegrationEvent(), cancellationToken);
                message.MarkPublished(DateTimeOffset.UtcNow);
                _logger.LogInformation("Published outbox event {EventId} ({EventType})", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Max(0, _options.RetryDelaySeconds));
                message.MarkFailed(DateTimeOffset.UtcNow.Add(delay), ex.GetType().Name);
                _logger.LogWarning(ex, "Could not publish outbox event {EventId}; it remains pending", message.Id);
            }
        }

        if (messages.Count != 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }
}
