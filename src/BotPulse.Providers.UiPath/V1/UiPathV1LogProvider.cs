using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.UiPath.Common;

namespace BotPulse.Providers.UiPath.V1;

internal sealed class UiPathV1LogProvider : ILogProvider
{
    private readonly UiPathHttpClient _http;
    public UiPathV1LogProvider(UiPathHttpClient http) => _http = http;

    public async Task<IReadOnlyList<ExecutionLogSnapshot>> GetExecutionLogsAsync(
        LogQuery query, CancellationToken ct = default)
    {
        var filters = new List<string>();
        if (!string.IsNullOrEmpty(query.JobExternalId))
        {
            filters.Add($"JobKey eq '{query.JobExternalId}'");
        }

        if (query.FromUtc.HasValue)
        {
            filters.Add($"TimeStamp gt {query.FromUtc.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }

        if (!string.IsNullOrEmpty(query.MinSeverity))
        {
            filters.Add($"Level eq '{query.MinSeverity}'");
        }

        var qs = $"$top={query.Top}&$orderby=TimeStamp desc";
        if (filters.Count > 0)
        {
            qs += $"&$filter={string.Join(" and ", filters)}";
        }

        var dtos = await _http.GetOdataAsync<UiPathLogDto>("odata/RobotLogs", qs, ct).ConfigureAwait(false);
        return dtos.Select(Map).ToList();
    }

    private static ExecutionLogSnapshot Map(UiPathLogDto dto) => new(
        JobExternalId: dto.JobKey,
        RobotExternalId: dto.RobotName,
        ProcessExternalId: dto.ProcessName,
        TimestampUtc: dto.TimeStamp,
        Severity: MapSeverity(dto.Level),
        LoggerName: dto.ProcessName ?? "UiPath",
        Message: dto.Message,
        PropertiesJson: dto.RawMessage ?? "{}");

    private static string MapSeverity(string? level) => level?.ToUpperInvariant() switch
    {
        "TRACE" or "DEBUG" => "Debug",
        "INFO" or "INFORMATION" => "Info",
        "WARN" or "WARNING" => "Warn",
        "ERROR" => "Error",
        "FATAL" or "CRITICAL" => "Fatal",
        _ => "Info",
    };

    private sealed class UiPathLogDto
    {
        public string? JobKey { get; init; }
        public string? RobotName { get; init; }
        public string? ProcessName { get; init; }
        public DateTime TimeStamp { get; init; }
        public string? Level { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? RawMessage { get; init; }
    }
}
