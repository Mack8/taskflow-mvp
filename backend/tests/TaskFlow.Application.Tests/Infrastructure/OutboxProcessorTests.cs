using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Messaging;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests.Infrastructure;

public class OutboxProcessorTests
{
    [Fact]
    public async Task Commit_PersistsDomainChangeAndOutboxMessageBeforeAnyBrokerCall()
    {
        await using var db = CreateContext();
        var task = TaskItem.Create(Guid.NewGuid(), "Deliver event", "", TaskPriority.Medium, null);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        task.AssignTo(Guid.NewGuid(), Guid.NewGuid());
        await db.CommitAsync();

        var message = (await db.OutboxMessages.ToListAsync()).Should().ContainSingle().Subject;
        message.EventType.Should().Be("TaskAssignedEvent");
        message.PublishedAt.Should().BeNull();
        message.Payload.Should().Contain("Deliver event");
    }

    [Fact]
    public async Task FailedPublish_RemainsPending_AndFreshProcessorRetriesWithSameEventId()
    {
        var databaseName = $"outbox-{Guid.NewGuid()}";
        var bus = new RecordingEventBus { ThrowOnPublish = true };
        Guid eventId;

        await using (var db = CreateContext(databaseName))
        {
            var task = TaskItem.Create(Guid.NewGuid(), "Recover event", "", TaskPriority.Medium, null);
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            task.ChangeStatus(TaskState.InProgress, Guid.NewGuid());
            await db.CommitAsync();
            eventId = (await db.OutboxMessages.ToListAsync()).Should().ContainSingle().Subject.Id;

            await CreateProcessor(db, bus).ProcessPendingAsync(CancellationToken.None);
            var pending = await db.OutboxMessages.SingleAsync();
            pending.PublishedAt.Should().BeNull();
            pending.AttemptCount.Should().Be(1);
        }

        bus.ThrowOnPublish = false;
        await using (var restartedDb = CreateContext(databaseName))
        {
            await CreateProcessor(restartedDb, bus).ProcessPendingAsync(CancellationToken.None);
            var published = await restartedDb.OutboxMessages.SingleAsync();
            published.PublishedAt.Should().NotBeNull();
            published.AttemptCount.Should().Be(1);
        }

        bus.Published.Should().ContainSingle().Which.EventId.Should().Be(eventId);
    }

    private static ApplicationDbContext CreateContext(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"outbox-{Guid.NewGuid()}")
            .Options);

    private static OutboxProcessor CreateProcessor(ApplicationDbContext db, RecordingEventBus bus) =>
        new(db, bus, Options.Create(new OutboxOptions { RetryDelaySeconds = 0 }), NullLogger<OutboxProcessor>.Instance);

    private sealed class RecordingEventBus : IEventBus
    {
        public bool ThrowOnPublish { get; set; }
        public List<IntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPublish) throw new InvalidOperationException("Broker unavailable");
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
