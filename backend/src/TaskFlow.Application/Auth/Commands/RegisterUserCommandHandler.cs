using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Auth.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RegisterUserCommandHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException($"Email \"{request.Email}\" is already registered.");
        }

        var user = User.Register(request.Name, request.Email, _passwordHasher.Hash(request.Password));

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var token = _tokenGenerator.GenerateToken(user);
        return new AuthResultDto(user.Id, user.Name, user.Email, user.Role.ToString(), token);
    }
}
