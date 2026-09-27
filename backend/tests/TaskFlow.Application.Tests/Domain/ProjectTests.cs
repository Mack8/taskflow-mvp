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

    [Fact]
    public void RemoveMember_ExistingMember_RemovesThem()
    {
        var project = Project.Create("TaskFlow MVP", "demo", Guid.NewGuid());
        var memberId = Guid.NewGuid();
        project.AddMember(memberId);

        project.RemoveMember(memberId);

        project.HasMember(memberId).Should().BeFalse();
    }

    [Fact]
    public void RemoveMember_Owner_Throws()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("TaskFlow MVP", "demo", ownerId);

        var act = () => project.RemoveMember(ownerId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveMember_NotAMember_IsANoOp()
    {
        var project = Project.Create("TaskFlow MVP", "demo", Guid.NewGuid());

        var act = () => project.RemoveMember(Guid.NewGuid());

        act.Should().NotThrow();
    }
}
