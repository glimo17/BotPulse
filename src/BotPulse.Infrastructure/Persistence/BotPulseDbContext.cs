using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for BotPulse.
/// Only contains DbSets for persisted entities.
/// Robots, Machines, Processes and Assets are NOT persisted (read on-demand).
/// </summary>
public sealed class BotPulseDbContext : DbContext
{
    public BotPulseDbContext(DbContextOptions<BotPulseDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<QueueItem> QueueItems => Set<QueueItem>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<MetricPoint> MetricsRaw => Set<MetricPoint>();
    public DbSet<MetricRollup> MetricsRollups => Set<MetricRollup>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<DashboardLayout> DashboardLayouts => Set<DashboardLayout>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BotPulseDbContext).Assembly);
    }
}
