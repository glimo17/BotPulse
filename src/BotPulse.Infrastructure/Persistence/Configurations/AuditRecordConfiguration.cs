using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(a => a.TimestampUtc).HasColumnName("timestamp_utc").IsRequired();
        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired().HasMaxLength(255);
        builder.Property(a => a.UserName).HasColumnName("user_name").IsRequired().HasMaxLength(255);
        builder.Property(a => a.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
        builder.Property(a => a.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(100);
        builder.Property(a => a.ResourceId).HasColumnName("resource_id").HasMaxLength(255);
        builder.Property(a => a.Outcome).HasColumnName("outcome").IsRequired().HasMaxLength(20);
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(a => a.DetailsJson).HasColumnName("details_json").HasColumnType("jsonb");
        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id").IsRequired().HasMaxLength(64);

        builder.HasIndex(a => a.TimestampUtc).IsDescending().HasDatabaseName("idx_audit_timestamp_desc");
        builder.HasIndex(a => new { a.UserId, a.TimestampUtc }).HasDatabaseName("idx_audit_user_timestamp");
        builder.HasIndex(a => new { a.Action, a.TimestampUtc }).HasDatabaseName("idx_audit_action_timestamp");
    }
}
