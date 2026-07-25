using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace BotPulse.Infrastructure.Authentication;

/// <summary>Configuration options for JWT session token generation and validation.</summary>
public sealed class JwtOptions
{
    /// <summary>Base64-encoded signing key (minimum 32 bytes = 44 Base64 chars).</summary>
    [Required]
    public string SigningKeyBase64 { get; init; } = string.Empty;

    /// <summary>JWT issuer claim.</summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>JWT audience claim.</summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Token expiration in minutes. Must be between 15 and 480.</summary>
    [Range(15, 480)]
    public int ExpirationMinutes { get; init; } = 60;
}

/// <summary>Validates JwtOptions at application startup.</summary>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKeyBase64))
        {
            return ValidateOptionsResult.Fail("JWT_SIGNING_KEY is required. Set Jwt__SigningKeyBase64 environment variable.");
        }

        try
        {
            var keyBytes = Convert.FromBase64String(options.SigningKeyBase64);
            if (keyBytes.Length < 32)
            {
                return ValidateOptionsResult.Fail("JWT_SIGNING_KEY must be at least 256 bits (32 bytes = 44 Base64 chars).");
            }
        }
        catch (FormatException)
        {
            return ValidateOptionsResult.Fail("JWT_SIGNING_KEY is not valid Base64.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("Jwt__Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Jwt__Audience is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
