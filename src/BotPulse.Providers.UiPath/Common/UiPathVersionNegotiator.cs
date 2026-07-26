using System.Globalization;
using BotPulse.Core.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace BotPulse.Providers.UiPath.Common;

/// <summary>Negotiates the UiPath Orchestrator API version at startup.</summary>
internal sealed class UiPathVersionNegotiator : IProviderVersionNegotiator
{
    private static readonly Action<ILogger, string, Exception?> LogVersion =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "VersionNegotiated"),
            "UiPath Orchestrator version negotiated: {Version}");

    private readonly UiPathHttpClient _http;
    private readonly ILogger<UiPathVersionNegotiator> _logger;

    public UiPathVersionNegotiator(UiPathHttpClient http, ILogger<UiPathVersionNegotiator> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ProviderVersion> NegotiateAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = await _http.GetOdataAsync<UiPathSettingDto>(
                "odata/Settings", "$filter=Name eq 'Deployment.ProductVersion'", ct)
                .ConfigureAwait(false);

            var version = settings.Count > 0 ? settings[0].Value : "unknown";
            LogVersion(_logger, version, null);
            return new ProviderVersion("UiPath", version, "V1");
        }
        catch (Exception)
        {
            // If version endpoint fails, assume V1 (compatible with all modern Orchestrator versions)
            LogVersion(_logger, "unknown (defaulting to V1)", null);
            return new ProviderVersion("UiPath", "unknown", "V1");
        }
    }

    private sealed class UiPathSettingDto
    {
        public string Name { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
    }
}
