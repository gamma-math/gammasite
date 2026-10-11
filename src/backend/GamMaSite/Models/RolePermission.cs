using Microsoft.AspNetCore.Identity;

namespace GamMaSite.Models
{
    /* Connects one Identity role to one application permission. */
    public class RolePermission
    {
        public string RoleId { get; set; }

        public int PermissionId { get; set; }

        public IdentityRole Role { get; set; }

        public Permission Permission { get; set; }
    }
}
