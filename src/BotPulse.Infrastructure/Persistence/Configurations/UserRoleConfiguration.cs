using BotPulse.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Property(ur => ur.UserId).HasColumnName("user_id");
        builder.Property(ur => ur.RoleId).HasColumnName("role_id");
        builder.Property(ur => ur.AssignedByUserId).HasColumnName("assigned_by_user_id");
        builder.Property(ur => ur.AssignedAtUtc).HasColumnName("assigned_at_utc").IsRequired();
        builder.Property(ur => ur.Scope).HasColumnName("scope").HasMaxLength(255);

        builder.HasIndex(ur => ur.UserId).HasDatabaseName("idx_user_roles_user_id");
        builder.HasIndex(ur => ur.RoleId).HasDatabaseName("idx_user_roles_role_id");
    }
}
