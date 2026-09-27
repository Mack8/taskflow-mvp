namespace TaskFlow.Application.DTOs;

public record AuthResultDto(Guid UserId, string Name, string Email, string Role, string Token);
