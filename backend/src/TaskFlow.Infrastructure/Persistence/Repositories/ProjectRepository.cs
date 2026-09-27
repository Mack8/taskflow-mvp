using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public class ProjectRepository : RepositoryBase<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public override Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<List<Project>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Project?> GetWithTasksAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
}
