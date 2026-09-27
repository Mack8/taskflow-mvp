using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    // Backs the "first registered user becomes Admin" bootstrap rule in
    // RegisterUserCommandHandler — there's no seed data and no separate setup
    // step, so something has to answer "does an admin already need to exist?".
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
}
