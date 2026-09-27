using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Notifications.Models;
using TaskFlow.Notifications.Services;
using Xunit;

namespace TaskFlow.Notifications.IntegrationTests;

public class NotificationsFactory : WebApplicationFactory<NotificationsProgram>
{
    public const string SigningKey = "notifications-integration-test-key-1234567890";
    private readonly string _connection = Environment.GetEnvironmentVariable("TEST_MONGO_CONNECTION") ?? "mongodb://localhost:27017";
    private readonly string _database = $"taskflow_tests_{Guid.NewGuid():N}";

    public IMongoCollection<Notification> Collection => new MongoClient(_connection)
        .GetDatabase(_database).GetCollection<Notification>("notifications");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = SigningKey,
            ["Jwt:Issuer"] = "TaskFlow",
            ["Jwt:Audience"] = "TaskFlowClient",
            ["Mongo:ConnectionString"] = _connection,
            ["Mongo:Database"] = _database
        }));
        builder.ConfigureServices(services =>
        {
            // HTTP authorization and real Mongo persistence are under test; no broker is needed.
            var consumer = services.Single(d => d.ImplementationType == typeof(NotificationEventConsumer));
            services.Remove(consumer);
        });
    }

    public HttpClient ForUser(User user)
    {
        var generator = new JwtTokenGenerator(Options.Create(new JwtOptions { SecretKey = SigningKey }));
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", generator.GenerateToken(user));
        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await new MongoClient(_connection).DropDatabaseAsync(_database);
    }
}

public class NotificationEndpointsTests : IClassFixture<NotificationsFactory>
{
    private readonly NotificationsFactory _factory;
    public NotificationEndpointsTests(NotificationsFactory factory) => _factory = factory;

    private static User NewUser(UserRole role = UserRole.Member) =>
        User.Register("Test user", $"{Guid.NewGuid():N}@example.test", "unused-password-hash", role);

    [Fact]
    public async Task OwnNotifications_ApiIssuedToken_CanListAndMarkRead()
    {
        var owner = NewUser();
        var own = new Notification { UserId = owner.Id, Message = "Owned" };
        var foreign = new Notification { UserId = Guid.NewGuid(), Message = "Private" };
        await _factory.Collection.InsertManyAsync(new[] { own, foreign });
        using var client = _factory.ForUser(owner);

        var response = await client.GetAsync($"/api/notifications/{owner.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<Notification>>();
        Assert.Equal(own.Id, Assert.Single(notifications!).Id);

        var marked = await client.PatchAsync($"/api/notifications/{own.Id}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, marked.StatusCode);
        Assert.True((await _factory.Collection.Find(n => n.Id == own.Id).SingleAsync()).IsRead);
        // Marking an already-read notification remains successful.
        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsync($"/api/notifications/{own.Id}/read", null)).StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Member)]
    [InlineData(UserRole.Admin)]
    public async Task OtherUsersNotifications_CannotBeListedOrModified(UserRole role)
    {
        var foreign = new Notification { UserId = Guid.NewGuid(), Message = "Private" };
        await _factory.Collection.InsertOneAsync(foreign);
        using var client = _factory.ForUser(NewUser(role));

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/notifications/{foreign.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsync($"/api/notifications/{foreign.Id}/read", null)).StatusCode);
        Assert.False((await _factory.Collection.Find(n => n.Id == foreign.Id).SingleAsync()).IsRead);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PatchAsync($"/api/notifications/{ObjectId.GenerateNewId()}/read", null)).StatusCode);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("signature")]
    [InlineData("expired")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("identity")]
    [InlineData("empty-identity")]
    [InlineData("missing-identity")]
    public async Task InvalidCredentials_AreRejectedOnBothEndpoints(string kind)
    {
        using var client = _factory.CreateClient();
        if (kind != "missing")
        {
            var subject = kind == "identity" ? "not-a-guid" : kind == "empty-identity" ? Guid.Empty.ToString() : Guid.NewGuid().ToString();
            var claims = kind == "missing-identity" ? Array.Empty<Claim>() : new[] { new Claim(JwtRegisteredClaimNames.Sub, subject) };
            var key = kind == "signature" ? "different-integration-test-signing-key-1234567890" : NotificationsFactory.SigningKey;
            var token = new JwtSecurityToken(
                issuer: kind == "issuer" ? "OtherIssuer" : "TaskFlow",
                audience: kind == "audience" ? "OtherAudience" : "TaskFlowClient",
                claims: claims,
                expires: kind == "expired" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(10),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                kind == "malformed" ? "not-a-token" : new JwtSecurityTokenHandler().WriteToken(token));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/notifications/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsync($"/api/notifications/{ObjectId.GenerateNewId()}/read", null)).StatusCode);
    }

    [Fact]
    public async Task InvalidNotificationId_ReturnsBadRequest()
    {
        using var client = _factory.ForUser(NewUser());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsync("/api/notifications/not-an-object-id/read", null)).StatusCode);
    }

    [Fact]
    public async Task Health_RemainsAnonymous()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
