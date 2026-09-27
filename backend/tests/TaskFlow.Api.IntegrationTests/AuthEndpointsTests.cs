using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskFlow.Application.DTOs;
using Xunit;

namespace TaskFlow.Api.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsMatchingUser()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@taskflow.dev";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Name = "Ada Lovelace",
            Email = email,
            Password = "SuperSecret123!"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        registered!.Token.Should().NotBeNullOrWhiteSpace();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "SuperSecret123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        loggedIn!.UserId.Should().Be(registered.UserId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@taskflow.dev";
        var payload = new { Name = "Ada", Email = email, Password = "SuperSecret123!" };

        await client.PostAsJsonAsync("/api/auth/register", payload);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", payload);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ProjectsEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
