using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.RuleId).HasColumnName("rule_id").IsRequired();
        builder.Property(a => a.Severity).HasColumnName("severity").IsRequired().HasMaxLength(20);
        builder.Property(a => a.RaisedAtUtc).HasColumnName("raised_at_utc").IsRequired();
        builder.Property(a => a.ConditionDescription).HasColumnName("condition_description").IsRequired();
        builder.Property(a => a.AffectedResourceType).HasColumnName("affected_resource_type").IsRequired().HasMaxLength(100);
        builder.Property(a => a.AffectedResourceId).HasColumnName("affected_resource_id").IsRequired().HasMaxLength(255);
        builder.Property(a => a.Acknowledged).HasColumnName("acknowledged").HasDefaultValue(false);
        builder.Property(a => a.AcknowledgedBy).HasColumnName("acknowledged_by").HasMaxLength(255);
        builder.Property(a => a.AcknowledgedAtUtc).HasColumnName("acknowledged_at_utc");
        builder.Property(a => a.EscalationLevel).HasColumnName("escalation_level").HasDefaultValue(0);

        builder.HasIndex(a => a.RaisedAtUtc).IsDescending().HasDatabaseName("idx_alerts_raised");
        builder.HasIndex(a => new { a.Acknowledged, a.Severity }).HasDatabaseName("idx_alerts_ack_severity");
    }
}
