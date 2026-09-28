using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

// Invoked only by the operator's CLI command, never by public registration or
// normal server startup. The database transaction owns the cross-process lock.
public class AdminBootstrapper(ApplicationDbContext db, IPasswordHasher passwordHasher)
{
    public async Task<bool> InitializeAsync(string name, string email, string password, CancellationToken ct = default)
    {
        await new RegisterUserCommandValidator().ValidateAndThrowAsync(
            new RegisterUserCommand(name, email, password), ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = N'TaskFlow.InitialAdmin',
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            IF @result < 0 THROW 51000, 'Could not acquire administrator initialization lock.', 1;
            """, ct);

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct)) return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
            throw new ConflictException("The bootstrap email is already registered. Existing accounts are never promoted by bootstrap.");

        db.Users.Add(User.Register(name, normalizedEmail, passwordHasher.Hash(password), UserRole.Admin));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}
