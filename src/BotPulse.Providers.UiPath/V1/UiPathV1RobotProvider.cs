using System.Globalization;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.UiPath.Common;

namespace BotPulse.Providers.UiPath.V1;

internal sealed class UiPathV1RobotProvider : IRobotProvider
{
    private readonly UiPathHttpClient _http;
    public UiPathV1RobotProvider(UiPathHttpClient http) => _http = http;

    public async Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathRobotDto>("odata/Robots", ct: ct).ConfigureAwait(false);
        return dtos.Select(Map).ToList();
    }

    public async Task<RobotSnapshot?> GetRobotByIdAsync(string externalId, CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathRobotDto>(
            "odata/Robots", $"$filter=Id eq {externalId}", ct).ConfigureAwait(false);
        return dtos.Count > 0 ? Map(dtos[0]) : null;
    }

    private static RobotSnapshot Map(UiPathRobotDto dto) => new(
        ExternalId: dto.Id.ToString(CultureInfo.InvariantCulture),
        Name: dto.Name,
        Status: MapStatus(dto.ExecutionStatus),
        MachineExternalId: dto.MachineId?.ToString(CultureInfo.InvariantCulture),
        LicenseType: dto.LicenseType,
        LastHeartbeatUtc: dto.ReportingTime ?? DateTime.UtcNow);

    private static string MapStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "IDLE" => "Idle",
        "BUSY" => "Busy",
        "DISCONNECTED" => "Offline",
        "UNRESPONSIVE" => "Offline",
        _ => "Online",
    };

    private sealed class UiPathRobotDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ExecutionStatus { get; init; }
        public long? MachineId { get; init; }
        public string? LicenseType { get; init; }
        public DateTime? ReportingTime { get; init; }
    }
}
