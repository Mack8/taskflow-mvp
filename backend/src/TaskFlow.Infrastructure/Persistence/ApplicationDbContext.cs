using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    private readonly IEventBus _eventBus;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IEventBus eventBus) : base(options)
    {
        _eventBus = eventBus;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    // Unit of Work entry point used by every command handler. Domain events
    // recorded on aggregates during this unit of work are only published to
    // the message broker AFTER SaveChanges succeeds — publishing before commit
    // could tell the Notifications service about a change that then rolls back.
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();

        var result = await SaveChangesAsync(cancellationToken);

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                await _eventBus.PublishAsync(domainEvent, cancellationToken);
            }
        }

        return result;
    }
}
