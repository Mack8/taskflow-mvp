using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

// Join entity between Project and User; kept as its own aggregate member
// (not a plain many-to-many) because membership carries its own metadata
// (JoinedAt) that a bare join table would lose.
public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Project? Project { get; private set; }

    public Guid UserId { get; private set; }
    public User? User { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ProjectMember() { }

    public static ProjectMember Create(Guid projectId, Guid userId) => new()
    {
        ProjectId = projectId,
        UserId = userId
    };
}
