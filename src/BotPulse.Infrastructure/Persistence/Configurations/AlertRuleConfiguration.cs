using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
        builder.Property(r => r.RuleType).HasColumnName("rule_type").IsRequired().HasMaxLength(100);
        builder.Property(r => r.Enabled).HasColumnName("enabled").HasDefaultValue(true);
        builder.Property(r => r.Severity).HasColumnName("severity").IsRequired().HasMaxLength(20);
        builder.Property(r => r.ParametersJson).HasColumnName("parameters_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(r => r.ChannelsJson).HasColumnName("channels_json").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(r => r.EscalationEnabled).HasColumnName("escalation_enabled").HasDefaultValue(false);
        builder.Property(r => r.EscalationTimeoutMinutes).HasColumnName("escalation_timeout_minutes").HasDefaultValue(15);
        builder.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
    }
}
