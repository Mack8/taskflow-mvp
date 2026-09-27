namespace TaskFlow.Application.Common.Interfaces;

// Populated by the API layer from the caller's JWT claims. Handlers depend on
// this abstraction instead of HttpContext, so Application stays testable and
// framework-agnostic (it never sees ASP.NET Core types).
public interface ICurrentUserService
{
    Guid UserId { get; }
    string? Role { get; }
}
