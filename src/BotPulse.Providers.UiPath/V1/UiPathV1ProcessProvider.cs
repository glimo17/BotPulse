using System.Globalization;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.UiPath.Common;

namespace BotPulse.Providers.UiPath.V1;

internal sealed class UiPathV1ProcessProvider : IProcessProvider
{
    private readonly UiPathHttpClient _http;
    public UiPathV1ProcessProvider(UiPathHttpClient http) => _http = http;

    public async Task<IReadOnlyList<ProcessSnapshot>> GetProcessesAsync(CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathReleaseDto>("odata/Releases", ct: ct).ConfigureAwait(false);
        return dtos.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ProcessParameter>> GetProcessParametersAsync(
        string processExternalId, CancellationToken ct = default)
    {
        // UiPath Automation Cloud v2024+ exposes arguments via odata/Releases
        var dtos = await _http.GetOdataAsync<UiPathReleaseDto>(
            "odata/Releases", $"$filter=Id eq {processExternalId}&$expand=Arguments", ct)
            .ConfigureAwait(false);

        var release = dtos.Count > 0 ? dtos[0] : null;
        if (release?.Arguments is null)
        {
            return [];
        }

        return release.Arguments
            .Select(a => new ProcessParameter(a.Name, a.Type ?? "string", a.Required, a.DefaultValue))
            .ToList();
    }

    private static ProcessSnapshot Map(UiPathReleaseDto dto) => new(
        ExternalId: dto.Id.ToString(CultureInfo.InvariantCulture),
        Name: dto.Name,
        Version: dto.ProcessVersion ?? "1.0",
        PublicationStatus: "Published",
        Description: dto.Description,
        CompatibleRobotCount: 0);

    private sealed class UiPathReleaseDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ProcessVersion { get; init; }
        public string? Description { get; init; }
        public IReadOnlyList<UiPathArgumentDto>? Arguments { get; init; }
    }

    private sealed class UiPathArgumentDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Type { get; init; }
        public bool Required { get; init; }
        public string? DefaultValue { get; init; }
    }
}
