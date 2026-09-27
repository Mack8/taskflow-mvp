using System.Net;
using System.Text.Json;
using TaskFlow.Application.Common.Exceptions;

namespace TaskFlow.Api.Middleware;

// Central place that turns Application-layer exceptions into HTTP status codes,
// so command/query handlers can throw plain exceptions without knowing about
// HTTP at all — keeps Application free of ASP.NET Core references.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (HttpStatusCode.BadRequest, "Validation failed", (object?)validationException.Errors),
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            ForbiddenAccessException => (HttpStatusCode.Forbidden, exception.Message, null),
            ConflictException => (HttpStatusCode.Conflict, exception.Message, null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message, null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { title, status = (int)statusCode, errors });
        await context.Response.WriteAsync(payload);
    }
}
