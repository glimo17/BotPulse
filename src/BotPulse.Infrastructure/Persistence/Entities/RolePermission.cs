using System;

namespace BotPulse.Infrastructure.Persistence.Entities
{
    internal sealed class RolePermission
    {
        public Guid RoleId { get; set; }
        public string Permission { get; set; } = string.Empty;
        public Role? Role { get; set; }
    }
}
