using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BotPulse.Infrastructure.Persistence.Entities;

namespace BotPulse.Infrastructure.Persistence.Configurations
{
    internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
            builder.Property(r => r.IsSystem).HasDefaultValue(false);

            builder.HasMany(r => r.UserRoles).WithOne(ur => ur.Role).HasForeignKey(ur => ur.RoleId);
            builder.HasMany(r => r.Permissions).WithOne(p => p.Role).HasForeignKey(p => p.RoleId);
        }
    }
}
