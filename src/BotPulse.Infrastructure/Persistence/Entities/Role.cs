using System;
using System.Collections.Generic;

namespace BotPulse.Infrastructure.Persistence.Entities
{
    internal sealed class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public List<UserRole> UserRoles { get; set; } = new();
        public List<RolePermission> Permissions { get; set; } = new();
    }
}
