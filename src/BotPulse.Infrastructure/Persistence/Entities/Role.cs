using System;
using System.Collections.Generic;

namespace BotPulse.Infrastructure.Persistence.Entities;

public sealed class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<UserRole> UserRoles { get; set; } = new();
    public List<RolePermissionEntry> Permissions { get; set; } = new();
}
