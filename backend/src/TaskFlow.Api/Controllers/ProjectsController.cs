using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Projects.Commands;
using TaskFlow.Application.Projects.Queries;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ISender _sender;

    public ProjectsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetProjects(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProjectsQuery(), cancellationToken));

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetProject(Guid projectId, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProjectByIdQuery(projectId), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProject), new { projectId = result.Id }, result);
    }

    [HttpPost("{projectId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> AddMember(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        await _sender.Send(new AddProjectMemberCommand(projectId, userId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{projectId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveProjectMemberCommand(projectId, userId), cancellationToken);
        return NoContent();
    }
}
