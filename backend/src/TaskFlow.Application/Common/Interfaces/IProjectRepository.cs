using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<List<Project>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Project?> GetWithTasksAsync(Guid projectId, CancellationToken cancellationToken = default);
}
