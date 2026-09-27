using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Tasks.Commands;
using TaskFlow.Application.Tasks.Queries;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class TasksController : ControllerBase
{
    private readonly ISender _sender;

    public TasksController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<List<TaskDto>>> GetTasksByProject(Guid projectId, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetTasksByProjectQuery(projectId), cancellationToken));

    [HttpPost("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<TaskDto>> CreateTask(Guid projectId, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTaskCommand(projectId, request.Title, request.Description, request.Priority, request.DueDate);
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTasksByProject), new { projectId }, result);
    }

    [HttpPatch("tasks/{taskId:guid}/status")]
    public async Task<ActionResult<TaskDto>> ChangeStatus(Guid taskId, [FromBody] ChangeTaskStatusCommand command, CancellationToken cancellationToken)
    {
        if (taskId != command.TaskId) return BadRequest("Route taskId does not match body.");
        return Ok(await _sender.Send(command, cancellationToken));
    }

    [HttpPatch("tasks/{taskId:guid}/assign")]
    public async Task<ActionResult<TaskDto>> Assign(Guid taskId, [FromBody] AssignTaskCommand command, CancellationToken cancellationToken)
    {
        if (taskId != command.TaskId) return BadRequest("Route taskId does not match body.");
        return Ok(await _sender.Send(command, cancellationToken));
    }
}

public record CreateTaskRequest(string Title, string Description, TaskPriority Priority, DateTimeOffset? DueDate);
