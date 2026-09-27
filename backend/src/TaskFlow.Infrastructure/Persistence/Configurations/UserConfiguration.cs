using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        // BaseEntity assigns Id client-side (Guid.NewGuid()) before the entity is
        // ever tracked. Without ValueGeneratedNever, EF Core sees a non-default key
        // on a newly-discovered entity and assumes it already exists in the
        // database, issuing an UPDATE instead of an INSERT (0 rows affected ->
        // DbUpdateConcurrencyException). Every entity in this model needs this.
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(u => u.DomainEvents);
    }
}
