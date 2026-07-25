using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IAlertRepository.</summary>
internal sealed class AlertRepository : GenericRepository<Alert>, IAlertRepository
{
    public AlertRepository(BotPulseDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Alert>> GetUnacknowledgedCriticalAsync(DateTime before, CancellationToken ct = default) =>
        await Context.Alerts
            .Where(a => !a.Acknowledged && a.Severity == "Critical" && a.RaisedAtUtc < before)
            .OrderBy(a => a.RaisedAtUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AcknowledgeAsync(Guid alertId, string userId, CancellationToken ct = default)
    {
        var alert = await Context.Alerts.FindAsync([alertId], ct).ConfigureAwait(false);
        if (alert is not null)
        {
            alert.Acknowledge(userId);
            Update(alert);
        }
    }
}

/// <summary>EF Core implementation of IAlertRuleRepository.</summary>
internal sealed class AlertRuleRepository : GenericRepository<AlertRule>, IAlertRuleRepository
{
    public AlertRuleRepository(BotPulseDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AlertRule>> GetEnabledAsync(CancellationToken ct = default) =>
        await Context.AlertRules
            .Where(r => r.Enabled)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
