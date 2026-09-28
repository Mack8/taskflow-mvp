using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Bootstrap.IntegrationTests;

public class SqlApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connection = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION") ??
        "Server=localhost,14339;User Id=sa;Password=BootstrapTest!12345;TrustServerCertificate=True")
    { InitialCatalog = $"taskflow_bootstrap_tests_{Guid.NewGuid():N}" }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Name"] = "Must not be created at startup",
            ["BootstrapAdmin:Email"] = "unused@example.test",
            ["BootstrapAdmin:Password"] = "UnusedPassword123!"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(_connection));
            services.RemoveAll<IEventBus>();
            services.AddSingleton<IEventBus, NoEvents>();
        });
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    public async Task<bool> BootstrapAsync(string email, string password = "BootstrapPassword123!", string name = "Initial admin")
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().InitializeAsync(name, email, password);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_connection).Options, new NoEvents());
        await db.Database.EnsureDeletedAsync();
    }

    private class NoEvents : IEventBus
    {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

public class AdminBootstrapTests
{
    [Fact]
    public async Task ConcurrentPublicRegistrations_AlwaysCreateMembers_EvenBeforeBootstrap()
    {
        await using var factory = new SqlApiFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => client.PostAsJsonAsync("/api/auth/register", new
        {
            Name = "Member", Email = $"public{i}@example.test", Password = "PublicPassword123!", Role = "Admin"
        })));
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("Member", (await response.Content.ReadFromJsonAsync<AuthResultDto>())!.Role);
        }
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(8, await db.Users.CountAsync());
        Assert.False(await db.Users.AnyAsync(u => u.Role == UserRole.Admin));
    }

    [Fact]
    public async Task ConcurrentBootstrapAndRegistration_CreateExactlyOneAdmin_WhoseCredentialsWork()
    {
        await using var factory = new SqlApiFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bootstraps = Enumerable.Range(0, 4).Select(async i =>
        {
            await start.Task;
            return await factory.BootstrapAsync($"admin{i}@example.test");
        }).ToArray();
        var registrations = Enumerable.Range(0, 4).Select(async i =>
        {
            await start.Task;
            return await client.PostAsJsonAsync("/api/auth/register", new
            { Name = "Member", Email = $"member{i}@example.test", Password = "MemberPassword123!" });
        }).ToArray();
        start.SetResult();
        Assert.Single((await Task.WhenAll(bootstraps)).Where(created => created));
        foreach (var response in await Task.WhenAll(registrations))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("Member", (await response.Content.ReadFromJsonAsync<AuthResultDto>())!.Role);
        }
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var admin = await db.Users.SingleAsync(u => u.Role == UserRole.Admin);
        Assert.Equal(5, await db.Users.CountAsync());
        var login = await client.PostAsJsonAsync("/api/auth/login", new { admin.Email, Password = "BootstrapPassword123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("Admin", (await login.Content.ReadFromJsonAsync<AuthResultDto>())!.Role);
    }

    [Fact]
    public async Task RepeatedBootstrap_DoesNotCreateOrModifyExistingAdmins()
    {
        await using var factory = new SqlApiFactory();
        await factory.InitializeAsync();
        Assert.True(await factory.BootstrapAsync("original@example.test"));
        Assert.False(await factory.BootstrapAsync("different@example.test"));
        Assert.False(await factory.BootstrapAsync("original@example.test", "ChangedPassword123!"));
        await using var scope = factory.Services.CreateAsyncScope();
        var admin = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.SingleAsync();
        Assert.Equal("original@example.test", admin.Email);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify("BootstrapPassword123!", admin.PasswordHash));
    }

    [Fact]
    public async Task ExistingMemberEmail_IsNeverPromoted_AndFailedAttemptReleasesLock()
    {
        await using var factory = new SqlApiFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        { Name = "Member", Email = "taken@example.test", Password = "MemberPassword123!" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await Assert.ThrowsAsync<ConflictException>(() => factory.BootstrapAsync("TAKEN@example.test"));
        Assert.True(await factory.BootstrapAsync("safe@example.test"));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(UserRole.Member, (await db.Users.SingleAsync(u => u.Email == "taken@example.test")).Role);
        Assert.Equal(1, await db.Users.CountAsync(u => u.Role == UserRole.Admin));
    }

    [Theory]
    [InlineData("", "valid@example.test", "Password123!")]
    [InlineData("Admin", "not-an-email", "Password123!")]
    [InlineData("Admin", "valid@example.test", "short")]
    public async Task InvalidConfiguration_DoesNotCreateAnAccount(string name, string email, string password)
    {
        await using var factory = new SqlApiFactory();
        await factory.InitializeAsync();
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => factory.BootstrapAsync(email, password, name));
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AnyAsync());
    }
}
