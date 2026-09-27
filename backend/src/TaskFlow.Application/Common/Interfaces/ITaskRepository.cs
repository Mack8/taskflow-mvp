using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<List<TaskItem>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
