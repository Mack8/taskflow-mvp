using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<List<Project>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Project?> GetWithTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

    // Admin-only: every project regardless of membership, so an admin can
    // assign any user to any project without first having to be added to it.
    Task<List<Project>> GetAllAsync(CancellationToken cancellationToken = default);
}
