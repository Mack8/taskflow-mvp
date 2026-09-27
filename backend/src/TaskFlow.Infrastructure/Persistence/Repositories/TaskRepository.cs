using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public class TaskRepository : RepositoryBase<TaskItem>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context) { }

    public Task<List<TaskItem>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        DbSet.Where(t => t.ProjectId == projectId).OrderBy(t => t.CreatedAt).ToListAsync(cancellationToken);
}
