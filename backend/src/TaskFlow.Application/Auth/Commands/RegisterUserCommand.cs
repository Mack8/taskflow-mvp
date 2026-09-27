using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Auth.Commands;

public record RegisterUserCommand(string Name, string Email, string Password) : IRequest<AuthResultDto>;
