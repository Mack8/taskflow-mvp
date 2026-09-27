using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using TaskFlow.Notifications.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddSingleton<NotificationRepository>();
builder.Services.AddHostedService<NotificationEventConsumer>();

// Must match the issuer, audience and signing key used by TaskFlow.Api.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = jwt["SecretKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new InvalidOperationException("Jwt:SecretKey must contain at least 32 UTF-8 bytes.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"] ?? "TaskFlow",
            ValidAudience = jwt["Audience"] ?? "TaskFlowClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                    || userId == Guid.Empty)
                    context.Fail("A valid user identity is required.");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).ValidateOnStart();
builder.Services.AddAuthorization();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var notifications = app.MapGroup("/api/notifications").RequireAuthorization();

notifications.MapGet("/{userId:guid}", async (Guid userId, ClaimsPrincipal user, NotificationRepository repository, CancellationToken ct) =>
{
    var currentUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (userId != currentUserId) return Results.Forbid();
    return Results.Ok(await repository.GetForUserAsync(currentUserId, ct));
});

notifications.MapPatch("/{id}/read", async (string id, ClaimsPrincipal user, NotificationRepository repository, CancellationToken ct) =>
{
    if (!ObjectId.TryParse(id, out _)) return Results.BadRequest();
    var currentUserId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await repository.MarkAsReadAsync(id, currentUserId, ct);
    return result.MatchedCount == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();

public partial class NotificationsProgram { }
