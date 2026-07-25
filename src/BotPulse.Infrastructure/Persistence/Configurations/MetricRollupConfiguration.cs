using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class MetricRollupConfiguration : IEntityTypeConfiguration<MetricRollup>
{
    public void Configure(EntityTypeBuilder<MetricRollup> builder)
    {
        builder.ToTable("metrics_rollups");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(m => m.BucketStartUtc).HasColumnName("bucket_start_utc").IsRequired();
        builder.Property(m => m.Granularity).HasColumnName("granularity").IsRequired().HasMaxLength(20);
        builder.Property(m => m.MetricName).HasColumnName("metric_name").IsRequired().HasMaxLength(100);
        builder.Property(m => m.SumValue).HasColumnName("sum_value").IsRequired();
        builder.Property(m => m.MinValue).HasColumnName("min_value").IsRequired();
        builder.Property(m => m.MaxValue).HasColumnName("max_value").IsRequired();
        builder.Property(m => m.AvgValue).HasColumnName("avg_value").IsRequired();
        builder.Property(m => m.CountValue).HasColumnName("count_value").IsRequired();
        builder.Property(m => m.DimensionsJson).HasColumnName("dimensions_json").HasColumnType("jsonb").HasDefaultValue("{}");

        builder.HasIndex(m => new { m.BucketStartUtc, m.MetricName, m.Granularity }).HasDatabaseName("idx_metrics_rollup_bucket");
    }
}
