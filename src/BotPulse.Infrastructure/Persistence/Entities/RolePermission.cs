using System;

namespace BotPulse.Infrastructure.Persistence.Entities;

public sealed class RolePermissionEntry
{
    public Guid RoleId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public Role? Role { get; set; }
}
