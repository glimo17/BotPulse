using System;
using System.Collections.Generic;

namespace BotPulse.Authorization.Entities
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
        public bool IsSystemRole { get; set; } = false;
    }
}
