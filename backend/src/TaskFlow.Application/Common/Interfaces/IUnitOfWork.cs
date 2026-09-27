namespace TaskFlow.Application.Common.Interfaces;

// Explicit Unit of Work over IApplicationDbContext: handlers commit through this
// (not through the repositories) so a command that touches several repositories
// still saves as a single transaction/SaveChanges call.
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
