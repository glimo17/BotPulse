using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace BotPulse.Infrastructure.Authentication;

/// <summary>
/// Password hasher using Argon2id with OWASP-recommended parameters.
/// t=3, m=64MiB, p=1. Uses FixedTimeEquals for timing-attack resistance.
/// </summary>
internal sealed class Argon2idPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 3;
    private const int MemorySize = 65536; // 64 MiB in KiB
    private const int DegreeOfParallelism = 1;

    /// <summary>Hashes a plaintext password and returns a storable string.</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>Verifies a plaintext password against a stored hash using constant-time comparison.</summary>
    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = ComputeHash(password, salt);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations,
        };
        return argon2.GetBytes(HashSize);
    }
}
