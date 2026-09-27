namespace TaskFlow.Application.DTOs;

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
