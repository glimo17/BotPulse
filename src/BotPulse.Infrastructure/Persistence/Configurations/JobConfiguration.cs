using BotPulse.Core.Domain.Entities;
using BotPulse.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotPulse.Infrastructure.Persistence.Configurations;

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id").UseIdentityColumn();
        builder.Property(j => j.ExternalJobId).HasColumnName("external_job_id").IsRequired().HasMaxLength(255);
        builder.Property(j => j.ProviderName).HasColumnName("provider_name").IsRequired().HasMaxLength(50);
        builder.Property(j => j.ProcessExternalId).HasColumnName("process_external_id").IsRequired().HasMaxLength(255);
        builder.Property(j => j.RobotExternalId).HasColumnName("robot_external_id").IsRequired().HasMaxLength(255);
        builder.Property(j => j.MachineExternalId).HasColumnName("machine_external_id").HasMaxLength(255);
        builder.Property(j => j.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(v => v.Value, v => JobStatus.Parse(v));
        builder.Property(j => j.StartTimeUtc).HasColumnName("start_time_utc").IsRequired();
        builder.Property(j => j.EndTimeUtc).HasColumnName("end_time_utc");
        builder.Property(j => j.Duration).HasColumnName("duration");
        builder.Property(j => j.ErrorType).HasColumnName("error_type").HasMaxLength(255);
        builder.Property(j => j.ErrorMessage).HasColumnName("error_message");
        builder.Property(j => j.RetryOfJobId).HasColumnName("retry_of_job_id");
        builder.Property(j => j.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(j => j.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasIndex(j => new { j.ProviderName, j.ExternalJobId }).IsUnique().HasDatabaseName("idx_jobs_provider_external_unique");
        builder.HasIndex(j => j.Status).HasDatabaseName("idx_jobs_status");
        builder.HasIndex(j => j.StartTimeUtc).IsDescending().HasDatabaseName("idx_jobs_start_time_desc");
        builder.HasIndex(j => j.RobotExternalId).HasDatabaseName("idx_jobs_robot");
        builder.HasIndex(j => j.ProcessExternalId).HasDatabaseName("idx_jobs_process");
    }
}
