using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

internal sealed class DemoLogProvider : ILogProvider
{
    private readonly DemoDataSeed _seed;
    public DemoLogProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<ExecutionLogSnapshot>> GetExecutionLogsAsync(LogQuery query, CancellationToken ct = default)
    {
        var logs = _seed.GetLogs().AsEnumerable();

        if (!string.IsNullOrEmpty(query.JobExternalId))
        {
            logs = logs.Where(l => l.JobExternalId == query.JobExternalId);
        }

        if (query.FromUtc.HasValue)
        {
            logs = logs.Where(l => l.TimestampUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            logs = logs.Where(l => l.TimestampUtc <= query.ToUtc.Value);
        }

        if (query.Top > 0)
        {
            logs = logs.Take(query.Top);
        }

        return Task.FromResult<IReadOnlyList<ExecutionLogSnapshot>>(logs.ToList());
    }
}
