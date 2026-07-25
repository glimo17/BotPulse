using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
    public void Configure(EntityTypeBuilder<ExecutionLog> builder)
    {
        builder.ToTable("execution_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(l => l.TimestampUtc).HasColumnName("timestamp_utc").IsRequired();
        builder.Property(l => l.Severity).HasColumnName("severity").IsRequired().HasMaxLength(20);
        builder.Property(l => l.LoggerName).HasColumnName("logger_name").IsRequired().HasMaxLength(255);
        builder.Property(l => l.Message).HasColumnName("message").IsRequired();
        builder.Property(l => l.JobExternalId).HasColumnName("job_external_id").HasMaxLength(255);
        builder.Property(l => l.RobotExternalId).HasColumnName("robot_external_id").HasMaxLength(255);
        builder.Property(l => l.ProcessExternalId).HasColumnName("process_external_id").HasMaxLength(255);
        builder.Property(l => l.PropertiesJson).HasColumnName("properties_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(l => l.ProviderName).HasColumnName("provider_name").IsRequired().HasMaxLength(50);
        builder.Property(l => l.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(l => l.TimestampUtc).IsDescending().HasDatabaseName("idx_logs_timestamp_desc");
        builder.HasIndex(l => new { l.JobExternalId, l.TimestampUtc }).HasDatabaseName("idx_logs_job_timestamp");
        builder.HasIndex(l => new { l.Severity, l.TimestampUtc }).HasDatabaseName("idx_logs_severity");
    }
}
