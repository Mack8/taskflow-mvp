using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Api.IntegrationTests;

// Environment is set to "Testing" (not "Development") so Program.cs skips the
// Swagger + db.Database.Migrate() block — Migrate() is a relational-only
// operation and throws against the InMemory provider swapped in below.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeEventBus EventBus { get; } = new();

    // Fixed per factory instance and captured by reference below. AddDbContext
    // registers DbContextOptions<T> as Scoped, so its configure delegate runs
    // once per request scope — inlining Guid.NewGuid() there would hand every
    // request a fresh, empty InMemory database instead of one shared per test.
    private readonly string _databaseName = $"taskflow-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IEventBus>();
            services.AddSingleton<IEventBus>(EventBus);

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task<User> CreateUserAsync(UserRole role = UserRole.Member)
    {
        await using var scope = Services.CreateAsyncScope();
        var user = User.Register($"{role} user", $"{Guid.NewGuid():N}@taskflow.test", "test-hash", role);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public HttpClient CreateAuthenticatedClient(User user)
    {
        var client = CreateClient();
        var token = Services.GetRequiredService<IJwtTokenGenerator>().GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
