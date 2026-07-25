using BotPulse.Core.Domain.ValueObjects;

namespace BotPulse.Core.Domain.Entities;

/// <summary>BotPulse platform user with role-based authorization.</summary>
public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string AuthProvider { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static User Create(string externalId, string userName, string email, UserRole role, string authProvider, string? passwordHash = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            UserName = userName,
            Email = email,
            Role = role,
            AuthProvider = authProvider,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordLogin() => LastLoginUtc = DateTime.UtcNow;
    public void Deactivate() => IsActive = false;
}
