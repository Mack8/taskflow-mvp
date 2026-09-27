using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Auth.Commands;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthResultDto>;
