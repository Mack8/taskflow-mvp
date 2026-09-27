using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public User? Owner { get; private set; }

    private readonly List<ProjectMember> _members = new();
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private readonly List<TaskItem> _tasks = new();
    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

    private Project() { }

    public static Project Create(string name, string description, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        var project = new Project
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            OwnerId = ownerId
        };
        project._members.Add(ProjectMember.Create(project.Id, ownerId));
        return project;
    }

    public void AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId)) return;
        _members.Add(ProjectMember.Create(Id, userId));
        Touch();
    }

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);
}
