using System;

namespace BotPulse.Infrastructure.Persistence.Entities;

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Reserved for future folder-level security. Null = global assignment.</summary>
    public string? Scope { get; set; }

    public Role? Role { get; set; }
}
