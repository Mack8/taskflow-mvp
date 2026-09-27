using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Users.Queries;

public record GetUsersQuery : IRequest<List<UserSummaryDto>>;
