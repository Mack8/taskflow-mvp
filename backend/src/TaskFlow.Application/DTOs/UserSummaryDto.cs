namespace TaskFlow.Application.DTOs;

public record UserSummaryDto(Guid Id, string Name, string Email, string Role, DateTimeOffset CreatedAt);
