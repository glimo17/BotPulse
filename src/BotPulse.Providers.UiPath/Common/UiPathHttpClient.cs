using System.Net.Http.Json;
using System.Text.Json;
using BotPulse.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Providers.UiPath.Common;

/// <summary>
/// Typed HTTP client for UiPath Orchestrator OData API.
/// Handles authentication header injection and error translation.
/// </summary>
internal sealed class UiPathHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Action<ILogger, int, string, string, Exception?> LogApiError =
        LoggerMessage.Define<int, string, string>(LogLevel.Error, new EventId(1, "UiPathApiError"),
            "UiPath API error {StatusCode} at {Url}: {Body}");

    private readonly HttpClient _http;
    private readonly UiPathOAuth2TokenManager _tokens;
    private readonly UiPathOptions _options;
    private readonly ILogger<UiPathHttpClient> _logger;

    public UiPathHttpClient(
        HttpClient http,
        UiPathOAuth2TokenManager tokens,
        IOptions<UiPathOptions> options,
        ILogger<UiPathHttpClient> logger)
    {
        _http = http;
        _tokens = tokens;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Executes a GET against an OData endpoint and deserializes the value array.</summary>
    public async Task<IReadOnlyList<T>> GetOdataAsync<T>(
        string path,
        string? queryString = null,
        CancellationToken ct = default)
    {
        var token = await _tokens.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var url = BuildUrl(path, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-UIPATH-TenantName", _options.Tenant);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url, ct).ConfigureAwait(false);

        var envelope = await response.Content
            .ReadFromJsonAsync<ODataEnvelope<T>>(JsonOptions, ct)
            .ConfigureAwait(false);

        return envelope?.Value ?? [];
    }

    /// <summary>Executes a POST and returns the deserialized response.</summary>
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken ct = default)
    {
        var token = await _tokens.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var url = BuildUrl(path);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-UIPATH-TenantName", _options.Tenant);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url, ct).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync<TResponse>(JsonOptions, ct)
            .ConfigureAwait(false);

        return result ?? throw new ProviderException("UiPath", $"Empty response from POST {path}");
    }

    /// <summary>Executes a POST with no response body expected.</summary>
    public async Task PostAsync<TRequest>(string path, TRequest body, CancellationToken ct = default)
    {
        var token = await _tokens.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var url = BuildUrl(path);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-UIPATH-TenantName", _options.Tenant);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url, ct).ConfigureAwait(false);
    }

    private string BuildUrl(string path, string? queryString = null)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var tenant = _options.Tenant;
        var fullPath = $"{baseUrl}/{tenant}/orchestrator_/{path.TrimStart('/')}";
        return queryString is not null ? $"{fullPath}?{queryString}" : fullPath;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string url, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        LogApiError(_logger, (int)response.StatusCode, url, body, null);

        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => new AuthenticationException(
                "UiPath returned 401. Check ClientId/ClientSecret and token scopes."),
            System.Net.HttpStatusCode.Forbidden => new AuthorizationException(
                $"UiPath returned 403 for {url}. Check OAuth2 scopes."),
            System.Net.HttpStatusCode.NotFound => new EntityNotFoundException("UiPath resource", url),
            _ => new ProviderException("UiPath",
                $"UiPath API returned {(int)response.StatusCode} for {url}. Body: {body}"),
        };
    }
}

/// <summary>OData v4 response envelope.</summary>
internal sealed class ODataEnvelope<T>
{
    public IReadOnlyList<T>? Value { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("@odata.count")]
    public int? Count { get; init; }
}
