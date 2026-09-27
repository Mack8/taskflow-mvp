using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(cancellationToken);

    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        DbSet.OrderBy(u => u.Name).ToListAsync(cancellationToken);
}
