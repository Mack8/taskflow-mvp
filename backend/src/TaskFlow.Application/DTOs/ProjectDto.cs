namespace TaskFlow.Application.DTOs;

public record ProjectDto(Guid Id, string Name, string Description, Guid OwnerId, int MemberCount, int TaskCount, DateTimeOffset CreatedAt);

public record ProjectDetailDto(Guid Id, string Name, string Description, Guid OwnerId, List<Guid> MemberIds, List<TaskDto> Tasks);
