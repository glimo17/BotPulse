using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BotPulse.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Providers.UiPath.Common;

/// <summary>
/// Manages OAuth2 Client Credentials tokens for UiPath Orchestrator.
/// Caches the token in memory and refreshes it before expiry.
/// </summary>
internal sealed class UiPathOAuth2TokenManager : IDisposable
{
    private static readonly Action<ILogger, Exception?> LogTokenObtained =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "TokenObtained"),
            "UiPath OAuth2 token obtained successfully");

    private static readonly Action<ILogger, Exception?> LogTokenRefreshed =
        LoggerMessage.Define(LogLevel.Debug, new EventId(2, "TokenRefreshed"),
            "UiPath OAuth2 token refreshed");

    private static readonly Action<ILogger, Exception> LogTokenFailed =
        LoggerMessage.Define(LogLevel.Critical, new EventId(3, "TokenFailed"),
            "UiPath OAuth2 token acquisition failed. Provider is unhealthy.");

    private readonly HttpClient _http;
    private readonly UiPathOptions _options;
    private readonly ILogger<UiPathOAuth2TokenManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private bool _isHealthy = true;

    public UiPathOAuth2TokenManager(
        HttpClient http,
        IOptions<UiPathOptions> options,
        ILogger<UiPathOAuth2TokenManager> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Returns a valid access token, refreshing if necessary.</summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null &&
            DateTime.UtcNow < _tokenExpiry.AddSeconds(-_options.TokenSkewSeconds))
        {
            return _cachedToken;
        }

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cachedToken is not null &&
                DateTime.UtcNow < _tokenExpiry.AddSeconds(-_options.TokenSkewSeconds))
            {
                return _cachedToken;
            }

            return await FetchTokenAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Returns true if the last token acquisition was successful.</summary>
    public bool IsHealthy => _isHealthy;

    public void Dispose() => _lock.Dispose();

    private async Task<string> FetchTokenAsync(CancellationToken ct)
    {
        // UiPath Automation Cloud token endpoint is always at the root host (no org/tenant in path).
        // Structure: https://cloud.uipath.com/identity_/connect/token
        // For On-Prem Orchestrator the token URL is different; can be overridden via UiPath__TokenUrl.
        var tokenUrl = string.IsNullOrEmpty(_options.TokenUrl)
            ? $"{new Uri(_options.BaseUrl).GetLeftPart(UriPartial.Authority)}/identity_/connect/token"
            : _options.TokenUrl;

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
        };

        try
        {
            using var response = await _http
                .PostAsync(tokenUrl, new FormUrlEncodedContent(formData), ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new AuthenticationException(
                    $"UiPath OAuth2 token request failed ({(int)response.StatusCode}): {error}");
            }

            var tokenResponse = await response.Content
                .ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                .ConfigureAwait(false)
                ?? throw new ProviderException("UiPath", "Empty token response");

            var isRefresh = _cachedToken is not null;
            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            _isHealthy = true;

            if (isRefresh)
            {
                LogTokenRefreshed(_logger, null);
            }
            else
            {
                LogTokenObtained(_logger, null);
            }

            return _cachedToken;
        }
        catch (Exception ex) when (ex is not AuthenticationException && ex is not ProviderException)
        {
            _isHealthy = false;
            LogTokenFailed(_logger, ex);
            throw new ProviderException("UiPath", "Failed to obtain OAuth2 token", ex);
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = string.Empty;
    }
}
