using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using TaskFlow.Api.GraphQL;
using TaskFlow.Api.Middleware;
using TaskFlow.Application;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Persistence;

var bootstrapAdmin = args.Contains("--bootstrap-admin", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--bootstrap-admin").ToArray());

// Enums travel as their string names (e.g. "InProgress") over the wire in both
// directions, matching the string-typed enum columns in EF Core and the plain
// strings TaskDto/ProjectDto already expose through GraphQL.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddAuthorization();

// GraphQL clients in this MVP treat resolver failures as `errors` in a valid
// response. Legacy transport preserves the established application/json + 200
// contract for well-formed operations, including field-level authorization.
builder.Services.AddHttpResponseFormatter(new HttpResponseFormatterOptions
{
    HttpTransportVersion = HttpTransportVersion.Legacy
});

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (bootstrapAdmin)
{
    // Credentials come from operator-controlled configuration, not a public
    // endpoint. Schema migrations must be applied before this one-off command.
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        var config = app.Configuration.GetSection("BootstrapAdmin");
        var created = await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>()
            .InitializeAsync(config["Name"] ?? "", config["Email"] ?? "", config["Password"] ?? "");
        app.Logger.LogInformation(created ? "Initial administrator created." : "An administrator already exists; no changes made.");
    }
    catch (Exception ex)
    {
        // Avoid logging configuration values, passwords or SQL parameters.
        app.Logger.LogError("Administrator initialization failed ({ErrorType}). Check credentials, email availability, database access and applied migrations.", ex.GetType().Name);
        Environment.ExitCode = 1;
    }
    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGraphQL("/graphql").RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.Run();

public partial class Program { }
