using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.Api.IntegrationTests;

public class AuthorizationBoundariesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationBoundariesTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AnonymousCallers_CannotReachProtectedRestOrGraphqlEndpoints()
    {
        using var client = _factory.CreateClient();

        (await client.GetAsync("/api/projects")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/admin/users", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GraphqlAsync(client, "{ projects { id } }")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminOnlyRestAndGraphqlOperations_RejectMembersAndServeAdmins()
    {
        var member = await _factory.CreateUserAsync();
        var admin = await _factory.CreateUserAsync(UserRole.Admin);
        using var memberClient = _factory.CreateAuthenticatedClient(member);
        using var adminClient = _factory.CreateAuthenticatedClient(admin);
        var createUser = new { name = "Created", email = $"{Guid.NewGuid():N}@taskflow.test", password = "Password123!", role = "Member" };

        (await memberClient.PostAsJsonAsync("/api/admin/users", createUser)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await adminClient.PostAsJsonAsync("/api/admin/users", createUser)).StatusCode.Should().Be(HttpStatusCode.Created);

        var memberGraphql = await GraphqlAsync(memberClient, "{ users { id } allProjects { id } }");
        memberGraphql.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertGraphqlAuthorizationError(memberGraphql, "users", "allProjects");

        var adminGraphql = await GraphqlAsync(adminClient, "{ users { id } allProjects { id } }");
        adminGraphql.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminJson = await adminGraphql.Content.ReadFromJsonAsync<JsonElement>();
        adminJson.TryGetProperty("errors", out _).Should().BeFalse();
        adminJson.GetProperty("data").GetProperty("users").GetArrayLength().Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ProjectOwnerMemberAndOutsider_ReceiveTheirIntendedProjectAccess()
    {
        var owner = await _factory.CreateUserAsync();
        var member = await _factory.CreateUserAsync();
        var outsider = await _factory.CreateUserAsync();
        using var ownerClient = _factory.CreateAuthenticatedClient(owner);
        using var memberClient = _factory.CreateAuthenticatedClient(member);
        using var outsiderClient = _factory.CreateAuthenticatedClient(outsider);

        var created = await ownerClient.PostAsJsonAsync("/api/projects", new { name = "Restricted", description = "Private" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await created.Content.ReadFromJsonAsync<ProjectDto>())!;

        (await outsiderClient.GetAsync($"/api/projects/{project.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await outsiderClient.GetAsync($"/api/projects/{project.Id}/tasks")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await outsiderClient.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", new { title = "No access", description = "", priority = "Medium", dueDate = (string?)null })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertGraphqlProjectDenied(outsiderClient, $"query {{ project(projectId: \"{project.Id}\") {{ id }} }}");

        (await ownerClient.PostAsync($"/api/projects/{project.Id}/members/{member.Id}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await memberClient.GetAsync($"/api/projects/{project.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await memberClient.PostAsync($"/api/projects/{project.Id}/members/{outsider.Id}", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ownerClient.DeleteAsync($"/api/projects/{project.Id}/members/{owner.Id}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdminCanManageAnyProjectMembership_ButCannotReadTasksUntilAdded()
    {
        var owner = await _factory.CreateUserAsync();
        var admin = await _factory.CreateUserAsync(UserRole.Admin);
        var member = await _factory.CreateUserAsync();
        using var ownerClient = _factory.CreateAuthenticatedClient(owner);
        using var adminClient = _factory.CreateAuthenticatedClient(admin);

        var project = (await (await ownerClient.PostAsJsonAsync("/api/projects", new { name = "Owner project", description = "" }))
            .Content.ReadFromJsonAsync<ProjectDto>())!;
        (await adminClient.GetAsync($"/api/projects/{project.Id}/tasks")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await adminClient.PostAsync($"/api/projects/{project.Id}/members/{member.Id}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await adminClient.DeleteAsync($"/api/projects/{project.Id}/members/{member.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static Task<HttpResponseMessage> GraphqlAsync(HttpClient client, string query) =>
        client.PostAsJsonAsync("/graphql", new { query });

    private static async Task AssertGraphqlAuthorizationError(HttpResponseMessage response, params string[] paths)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = json.GetProperty("errors").EnumerateArray().ToList();
        errors.Should().HaveCount(paths.Length);
        foreach (var path in paths)
        {
            errors.Should().Contain(error => error.GetProperty("extensions").GetProperty("code").GetString() == "AUTH_NOT_AUTHORIZED" &&
                error.GetProperty("path")[0].GetString() == path);
        }
    }

    private static async Task AssertGraphqlProjectDenied(HttpClient client, string query)
    {
        var response = await GraphqlAsync(client, query);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
        // ProjectDetailDto is non-nullable in the schema. An execution error at
        // that field therefore propagates null to the root data object.
        json.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
