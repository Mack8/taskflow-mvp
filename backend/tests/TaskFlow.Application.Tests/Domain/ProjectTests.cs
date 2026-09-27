using FluentAssertions;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.Application.Tests.Domain;

public class ProjectTests
{
    [Fact]
    public void Create_AddsOwnerAsMember()
    {
        var ownerId = Guid.NewGuid();

        var project = Project.Create("TaskFlow MVP", "demo", ownerId);

        project.HasMember(ownerId).Should().BeTrue();
        project.Members.Should().ContainSingle();
    }

    [Fact]
    public void AddMember_Twice_IsIdempotent()
    {
        var project = Project.Create("TaskFlow MVP", "demo", Guid.NewGuid());
        var memberId = Guid.NewGuid();

        project.AddMember(memberId);
        project.AddMember(memberId);

        project.Members.Count(m => m.UserId == memberId).Should().Be(1);
    }
}
