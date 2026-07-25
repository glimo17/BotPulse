using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.ExternalId).HasColumnName("external_id").IsRequired().HasMaxLength(255);
        builder.Property(u => u.UserName).HasColumnName("user_name").IsRequired().HasMaxLength(255);
        builder.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
        builder.Property(u => u.Role).HasColumnName("role").IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.AuthProvider).HasColumnName("auth_provider").IsRequired().HasMaxLength(50);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(u => u.LastLoginUtc).HasColumnName("last_login_utc");
        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("idx_users_email_unique");
        builder.HasIndex(u => new { u.AuthProvider, u.ExternalId }).IsUnique().HasDatabaseName("idx_users_provider_external_unique");
    }
}
