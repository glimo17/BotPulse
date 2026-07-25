using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class QueueItemConfiguration : IEntityTypeConfiguration<QueueItem>
{
    public void Configure(EntityTypeBuilder<QueueItem> builder)
    {
        builder.ToTable("queue_items");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(q => q.ExternalItemId).HasColumnName("external_item_id").IsRequired().HasMaxLength(255);
        builder.Property(q => q.ProviderName).HasColumnName("provider_name").IsRequired().HasMaxLength(50);
        builder.Property(q => q.QueueName).HasColumnName("queue_name").IsRequired().HasMaxLength(255);
        builder.Property(q => q.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
        builder.Property(q => q.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(q => q.ProcessingStartUtc).HasColumnName("processing_start_utc");
        builder.Property(q => q.ProcessingEndUtc).HasColumnName("processing_end_utc");
        builder.Property(q => q.OutputMetadataJson).HasColumnName("output_metadata_json").HasColumnType("jsonb");
        builder.Property(q => q.OriginalItemId).HasColumnName("original_item_id");
        builder.Property(q => q.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(q => q.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasIndex(q => new { q.ProviderName, q.ExternalItemId }).IsUnique().HasDatabaseName("idx_queue_items_unique");
        builder.HasIndex(q => new { q.QueueName, q.Status }).HasDatabaseName("idx_queue_items_queue_status");
        builder.HasIndex(q => q.UpdatedAtUtc).IsDescending().HasDatabaseName("idx_queue_items_updated");
    }
}
