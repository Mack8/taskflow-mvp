using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TaskFlow.Application.DTOs;
using Xunit;

namespace TaskFlow.Api.IntegrationTests;

public class TaskStatusValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TaskStatusValidationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ChangeStatus_WithUndefinedNumericValue_ReturnsBadRequestWithoutChangingTask()
    {
        var user = await _factory.CreateUserAsync();
        using var client = _factory.CreateAuthenticatedClient(user);

        var projectResponse = await client.PostAsJsonAsync("/api/projects", new { name = "Status validation", description = "" });
        var project = (await projectResponse.Content.ReadFromJsonAsync<ProjectDto>())!;
        var taskResponse = await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Keep status", description = "", priority = "Medium", dueDate = (string?)null });
        var task = (await taskResponse.Content.ReadFromJsonAsync<TaskDto>())!;

        var response = await client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { taskId = task.Id, newStatus = 99 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("errors").TryGetProperty("NewStatus", out _).Should().BeTrue();

        var tasksResponse = await client.GetAsync($"/api/projects/{project.Id}/tasks");
        var tasks = (await tasksResponse.Content.ReadFromJsonAsync<List<TaskDto>>())!;
        tasks.Should().ContainSingle().Which.Status.Should().Be("Todo");
    }
}
