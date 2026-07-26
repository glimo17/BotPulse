using System.Globalization;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.UiPath.Common;

namespace BotPulse.Providers.UiPath.V1;

internal sealed class UiPathV1JobProvider : IJobProvider
{
    private readonly UiPathHttpClient _http;
    public UiPathV1JobProvider(UiPathHttpClient http) => _http = http;

    public async Task<IReadOnlyList<JobSnapshot>> GetJobsAsync(JobQuery query, CancellationToken ct = default)
    {
        var filters = new List<string>();
        if (query.UpdatedSinceUtc.HasValue)
        {
            filters.Add($"CreationTime gt {query.UpdatedSinceUtc.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            filters.Add($"State eq '{MapStatusToUiPath(query.Status)}'");
        }

        if (!string.IsNullOrEmpty(query.RobotExternalId))
        {
            filters.Add($"Robot/Id eq {query.RobotExternalId}");
        }

        var qs = $"$top={query.Top}&$skip={query.Skip}&$orderby=CreationTime desc";
        if (filters.Count > 0)
        {
            qs += $"&$filter={string.Join(" and ", filters)}";
        }

        var dtos = await _http.GetOdataAsync<UiPathJobDto>("odata/Jobs", qs, ct).ConfigureAwait(false);
        return dtos.Select(Map).ToList();
    }

    public async Task<JobSnapshot?> GetJobByIdAsync(string externalId, CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathJobDto>(
            "odata/Jobs", $"$filter=Id eq {externalId}", ct).ConfigureAwait(false);
        return dtos.Count > 0 ? Map(dtos[0]) : null;
    }

    public async Task<StartJobResult> StartJobAsync(StartJobRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            startInfo = new
            {
                ReleaseKey = request.ProcessExternalId,
                RobotIds = request.RobotExternalId is not null
                    ? new[] { long.Parse(request.RobotExternalId, CultureInfo.InvariantCulture) }
                    : Array.Empty<long>(),
                Strategy = request.RobotExternalId is not null ? "Specific" : "JobsCount",
                JobsCount = 1,
                InputArguments = request.Parameters is not null
                    ? System.Text.Json.JsonSerializer.Serialize(request.Parameters)
                    : "{}",
            },
        };

        var response = await _http.PostAsync<object, UiPathStartJobResponse>(
            "odata/Jobs/UiPath.Server.Configuration.OData.StartJobs", body, ct)
            .ConfigureAwait(false);

        var jobId = response.Value?.Count > 0 ? response.Value[0].Id
            : throw new BotPulse.Core.Exceptions.ProviderException("UiPath", "StartJobs returned no job ID");

        return new StartJobResult(jobId.ToString(CultureInfo.InvariantCulture));
    }

    public async Task StopJobAsync(string externalId, CancellationToken ct = default)
    {
        var body = new { jobId = long.Parse(externalId, CultureInfo.InvariantCulture), strategy = "SoftStop" };
        await _http.PostAsync($"odata/Jobs({externalId})/UiPath.Server.Configuration.OData.StopJob", body, ct)
            .ConfigureAwait(false);
    }

    public async Task CancelJobAsync(string externalId, CancellationToken ct = default)
    {
        var body = new { jobId = long.Parse(externalId, CultureInfo.InvariantCulture), strategy = "Kill" };
        await _http.PostAsync($"odata/Jobs({externalId})/UiPath.Server.Configuration.OData.StopJob", body, ct)
            .ConfigureAwait(false);
    }

    private static JobSnapshot Map(UiPathJobDto dto)
    {
        var duration = dto.StartTime.HasValue && dto.EndTime.HasValue
            ? dto.EndTime.Value - dto.StartTime.Value
            : (TimeSpan?)null;

        return new JobSnapshot(
            ExternalId: dto.Id.ToString(CultureInfo.InvariantCulture),
            ProcessExternalId: dto.ReleaseName ?? dto.Id.ToString(CultureInfo.InvariantCulture),
            RobotExternalId: dto.Robot?.Id.ToString(CultureInfo.InvariantCulture) ?? "unknown",
            MachineExternalId: dto.HostMachineName,
            Status: MapState(dto.State),
            StartTimeUtc: dto.StartTime ?? dto.CreationTime,
            EndTimeUtc: dto.EndTime,
            Duration: duration,
            ErrorType: dto.Info?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true ? "Error" : null,
            ErrorMessage: dto.Info);
    }

    private static string MapState(string? state) => state?.ToUpperInvariant() switch
    {
        "PENDING" => "Pending",
        "RUNNING" => "Running",
        "SUCCESSFUL" => "Success",
        "FAULTED" => "Failed",
        "STOPPED" => "Stopped",
        "ABANDONED" or "DELETED" => "Cancelled",
        "SUSPENDED" => "Stopped",
        _ => "Pending",
    };

    private static string MapStatusToUiPath(string status) => status.ToUpperInvariant() switch
    {
        "PENDING" => "Pending",
        "RUNNING" => "Running",
        "SUCCESS" => "Successful",
        "FAILED" => "Faulted",
        "STOPPED" => "Stopped",
        "CANCELLED" => "Abandoned",
        _ => status,
    };

    private sealed class UiPathJobDto
    {
        public long Id { get; init; }
        public string? ReleaseName { get; init; }
        public string? State { get; init; }
        public DateTime CreationTime { get; init; }
        public DateTime? StartTime { get; init; }
        public DateTime? EndTime { get; init; }
        public string? HostMachineName { get; init; }
        public string? Info { get; init; }
        public UiPathRobotRef? Robot { get; init; }
    }

    private sealed class UiPathRobotRef
    {
        public long Id { get; init; }
    }

    private sealed class UiPathStartJobResponse
    {
        public IReadOnlyList<UiPathJobRef>? Value { get; init; }
    }

    private sealed class UiPathJobRef
    {
        public long Id { get; init; }
    }
}
