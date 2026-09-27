using FluentAssertions;
using Moq;
using TaskFlow.Application.Auth.Commands;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.Application.Tests.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();

    private RegisterUserCommandHandler CreateHandler() =>
        new(_users.Object, _unitOfWork.Object, _passwordHasher.Object, _tokenGenerator.Object);

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAndReturnsToken()
    {
        _users.Setup(r => r.EmailExistsAsync("new@user.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("Password123!")).Returns("hashed");
        _tokenGenerator.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("fake-jwt");

        var handler = CreateHandler();
        var result = await handler.Handle(new RegisterUserCommand("Ada", "new@user.com", "Password123!"), CancellationToken.None);

        result.Token.Should().Be("fake-jwt");
        result.Email.Should().Be("new@user.com");
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingEmail_ThrowsConflict()
    {
        _users.Setup(r => r.EmailExistsAsync("taken@user.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var act = () => handler.Handle(new RegisterUserCommand("Ada", "taken@user.com", "Password123!"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
