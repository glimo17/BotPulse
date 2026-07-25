using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>Specialized repository for Alert entities.</summary>
public interface IAlertRepository : IRepository<Alert>
{
    Task<IReadOnlyList<Alert>> GetUnacknowledgedCriticalAsync(DateTime before, CancellationToken ct = default);
    Task AcknowledgeAsync(Guid alertId, string userId, CancellationToken ct = default);
}

/// <summary>Specialized repository for AlertRule entities.</summary>
public interface IAlertRuleRepository : IRepository<AlertRule>
{
    Task<IReadOnlyList<AlertRule>> GetEnabledAsync(CancellationToken ct = default);
}
