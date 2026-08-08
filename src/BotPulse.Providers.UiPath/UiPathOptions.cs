using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace BotPulse.Providers.UiPath;

/// <summary>Configuration options for the UiPath Orchestrator provider.</summary>
public sealed class UiPathOptions
{
    /// <summary>Base URL of the UiPath Automation Cloud or On-Prem Orchestrator.
    /// Example: https://cloud.uipath.com/myorg</summary>
    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>UiPath tenant name (e.g. DefaultTenant).</summary>
    [Required]
    public string Tenant { get; init; } = string.Empty;

    /// <summary>OAuth2 Client ID of the external application.</summary>
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>OAuth2 Client Secret of the external application.</summary>
    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>HTTP request timeout in seconds. Must be between 5 and 300.</summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Seconds before token expiry to trigger refresh.</summary>
    public int TokenSkewSeconds { get; init; } = 60;

    /// <summary>
    /// Optional UiPath folder (Organization Unit) ID for folder-scoped requests.
    /// When set, adds X-UIPATH-OrganizationUnitId header to every API request.
    /// </summary>
    public long? FolderId { get; init; }

    /// <summary>
    /// Optional override for the OAuth2 token endpoint URL.
    /// Defaults to {BaseUrl host}/identity_/connect/token (Automation Cloud).
    /// Override for On-Prem Orchestrator instances with a different token URL.
    /// </summary>
    public string? TokenUrl { get; init; }
}

/// <summary>Validates UiPathOptions at application startup.</summary>
internal sealed class UiPathOptionsValidator : IValidateOptions<UiPathOptions>
{
    public ValidateOptionsResult Validate(string? name, UiPathOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail("UiPath__BaseUrl is required.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail($"UiPath__BaseUrl '{options.BaseUrl}' is not a valid URL.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            return ValidateOptionsResult.Fail("UiPath__ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return ValidateOptionsResult.Fail("UiPath__ClientSecret is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Tenant))
        {
            return ValidateOptionsResult.Fail("UiPath__Tenant is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
