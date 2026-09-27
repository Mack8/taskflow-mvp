using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();

        // WithMany(u => u.Memberships) (not a bare WithMany()) so EF Core ties this
        // FK to the same relationship backing User.Memberships, instead of treating
        // the collection nav as a second, undiscovered relationship (which was
        // producing a shadow "UserId1" column).
        builder.HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(m => m.DomainEvents);
    }
}
