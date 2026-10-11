using System.Collections.Generic;

namespace GamMaSite.Models
{
    /* Stable permission definition used by role access control. */
    public class Permission
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
