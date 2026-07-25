using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class DashboardLayoutConfiguration : IEntityTypeConfiguration<DashboardLayout>
{
    public void Configure(EntityTypeBuilder<DashboardLayout> builder)
    {
        builder.ToTable("dashboard_layouts");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(d => d.WidgetsJson).HasColumnName("widgets_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(d => d.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasIndex(d => d.UserId).IsUnique().HasDatabaseName("idx_dashboard_layouts_user_unique");
    }
}
