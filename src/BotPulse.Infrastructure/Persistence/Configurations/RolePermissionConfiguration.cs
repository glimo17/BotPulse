using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BotPulse.Infrastructure.Persistence.Entities;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionEntry>
{
    public void Configure(EntityTypeBuilder<RolePermissionEntry> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rp => new { rp.RoleId, rp.Permission });
        builder.Property(rp => rp.Permission).HasMaxLength(200).IsRequired();
    }
}
