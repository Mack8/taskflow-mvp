using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Users.Queries;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserSummaryDto>>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;

    public GetUsersQueryHandler(IUserRepository users, ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<List<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != nameof(UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only an admin can list users.");
        }

        var users = await _users.GetAllAsync(cancellationToken);
        return users.Select(u => new UserSummaryDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.CreatedAt)).ToList();
    }
}
