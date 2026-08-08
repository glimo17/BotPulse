namespace BotPulse.Core.Abstractions.Authentication;

/// <summary>
/// Abstraction for password hashing and verification.
/// Implementation uses Argon2id with OWASP-recommended parameters.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
