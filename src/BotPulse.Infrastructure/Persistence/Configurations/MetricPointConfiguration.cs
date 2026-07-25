using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class MetricPointConfiguration : IEntityTypeConfiguration<MetricPoint>
{
    public void Configure(EntityTypeBuilder<MetricPoint> builder)
    {
        builder.ToTable("metrics_raw");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(m => m.TimestampUtc).HasColumnName("timestamp_utc").IsRequired();
        builder.Property(m => m.MetricName).HasColumnName("metric_name").IsRequired().HasMaxLength(100);
        builder.Property(m => m.Value).HasColumnName("value").IsRequired();
        builder.Property(m => m.DimensionsJson).HasColumnName("dimensions_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(m => m.ProviderName).HasColumnName("provider_name").IsRequired().HasMaxLength(50);

        builder.HasIndex(m => new { m.MetricName, m.TimestampUtc }).IsDescending().HasDatabaseName("idx_metrics_raw_name_time");
    }
}
