using System;

namespace BotPulse.Authorization.Entities;

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Reserved for future folder-level security.
    /// Null means the role assignment is global (current MVP behavior).
    /// </summary>
    public string? Scope { get; set; }
}
