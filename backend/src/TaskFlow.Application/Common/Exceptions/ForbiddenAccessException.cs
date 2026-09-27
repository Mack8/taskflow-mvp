namespace TaskFlow.Application.Common.Exceptions;

// Thrown when an authenticated user is valid but not allowed to touch a
// specific resource (e.g. a non-member reading a project's tasks).
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You do not have access to this resource.")
        : base(message) { }
}
