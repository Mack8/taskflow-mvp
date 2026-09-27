using TaskFlow.Notifications.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddSingleton<NotificationRepository>();
builder.Services.AddHostedService<NotificationEventConsumer>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Deliberately unauthenticated for the MVP: in production this would sit
// behind the same JWT bearer scheme as TaskFlow.Api (or, on Azure, behind
// API Management/App Service EasyAuth) validating the same signing key.
app.MapGet("/api/notifications/{userId:guid}", async (Guid userId, NotificationRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetForUserAsync(userId, ct)));

app.MapPatch("/api/notifications/{id}/read", async (string id, NotificationRepository repository, CancellationToken ct) =>
{
    var result = await repository.MarkAsReadAsync(id, ct);
    return result.MatchedCount == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();
