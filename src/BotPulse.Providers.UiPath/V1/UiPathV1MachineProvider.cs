using System.Globalization;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.UiPath.Common;

namespace BotPulse.Providers.UiPath.V1;

internal sealed class UiPathV1MachineProvider : IMachineProvider
{
    private readonly UiPathHttpClient _http;
    public UiPathV1MachineProvider(UiPathHttpClient http) => _http = http;

    public async Task<IReadOnlyList<MachineSnapshot>> GetMachinesAsync(CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathMachineDto>("odata/Machines", ct: ct).ConfigureAwait(false);
        return dtos.Select(Map).ToList();
    }

    public async Task<MachineSnapshot?> GetMachineByIdAsync(string externalId, CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathMachineDto>(
            "odata/Machines", $"$filter=Id eq {externalId}", ct).ConfigureAwait(false);
        return dtos.Count > 0 ? Map(dtos[0]) : null;
    }

    private static MachineSnapshot Map(UiPathMachineDto dto) => new(
        ExternalId: dto.Id.ToString(CultureInfo.InvariantCulture),
        Name: dto.Name,
        Status: dto.IsConnected ? "Online" : "Offline",
        LastHeartbeatUtc: dto.LastModificationTime ?? DateTime.UtcNow,
        ConnectedRobotCount: 0);

    private sealed class UiPathMachineDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsConnected { get; init; }
        public DateTime? LastModificationTime { get; init; }
    }
}
