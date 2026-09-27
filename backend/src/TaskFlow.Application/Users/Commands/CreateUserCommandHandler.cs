using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Users.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserSummaryDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCommandHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<UserSummaryDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != nameof(UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only an admin can create users.");
        }

        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException($"Email \"{request.Email}\" is already registered.");
        }

        var user = User.Register(request.Name, request.Email, _passwordHasher.Hash(request.Password), request.Role);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new UserSummaryDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
    }
}
