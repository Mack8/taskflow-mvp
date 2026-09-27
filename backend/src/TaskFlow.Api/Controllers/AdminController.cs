using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Users.Commands;

namespace TaskFlow.Api.Controllers;

// Class-level [Authorize(Roles = "Admin")] gates every action here — a second
// check also runs inside each handler (see CreateUserCommandHandler), since
// this same command could in principle be invoked from somewhere that isn't
// behind this attribute.
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserSummaryDto>> CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreateUser), result);
    }
}
