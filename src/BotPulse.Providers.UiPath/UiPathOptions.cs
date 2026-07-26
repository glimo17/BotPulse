using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace BotPulse.Providers.UiPath;

/// <summary>Configuration options for the UiPath Orchestrator provider.</summary>
public sealed class UiPathOptions
{
    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string Tenant { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    [Range(5, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    public int TokenSkewSeconds { get; init; } = 60;
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
