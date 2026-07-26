using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Application.Logs;

/// <summary>Query service for persisted execution logs.</summary>
public sealed class LogQueryService
{
    private readonly ILogRepository _logs;

    public LogQueryService(ILogRepository logs) => _logs = logs;

    public async Task<IReadOnlyList<ExecutionLog>> GetLogsAsync(
        LogFilter filter, CancellationToken ct = default)
    {
        var predicate = BuildPredicate(filter);
        var results = await _logs.FindAllAsync(predicate, ct).ConfigureAwait(false);

        return results
            .OrderByDescending(l => l.TimestampUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();
    }

    private static System.Linq.Expressions.Expression<Func<ExecutionLog, bool>> BuildPredicate(LogFilter filter)
    {
        return log =>
            (filter.JobExternalId == null || log.JobExternalId == filter.JobExternalId) &&
            (filter.ProviderName == null || log.ProviderName == filter.ProviderName) &&
            (filter.MinSeverity == null || string.Compare(log.Severity, filter.MinSeverity, StringComparison.OrdinalIgnoreCase) >= 0) &&
            (filter.FromUtc == null || log.TimestampUtc >= filter.FromUtc.Value) &&
            (filter.ToUtc == null || log.TimestampUtc <= filter.ToUtc.Value) &&
            (filter.Keyword == null || log.Message.Contains(filter.Keyword));
    }
}

/// <summary>Filter criteria for querying persisted execution logs.</summary>
public sealed record LogFilter(
    string? JobExternalId = null,
    string? ProviderName = null,
    string? MinSeverity = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Keyword = null,
    int Page = 1,
    int PageSize = 100);
