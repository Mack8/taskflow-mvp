using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Users.Commands;

// Admin-only (enforced both by [Authorize(Roles = "Admin")] on the controller
// and inside the handler, since GraphQL resolvers don't go through that
// attribute). Unlike RegisterUserCommand, this never returns a token — an
// admin creating an account for someone else must never end up holding that
// person's session.
public record CreateUserCommand(string Name, string Email, string Password, UserRole Role) : IRequest<UserSummaryDto>;
