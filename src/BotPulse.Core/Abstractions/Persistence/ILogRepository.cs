using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>Specialized repository for ExecutionLog entities with batch insert support.</summary>
public interface ILogRepository : IRepository<ExecutionLog>
{
    Task<DateTime?> GetMaxTimestampAsync(string providerName, CancellationToken ct = default);
    Task AddBatchAsync(IEnumerable<ExecutionLogSnapshot> snapshots, string providerName, CancellationToken ct = default);
}
