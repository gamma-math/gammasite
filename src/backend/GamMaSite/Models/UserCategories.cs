using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace GamMaSite.Models
{
    public class UserCategories
    {
        public UserStatus[] Status { get; private set; }
        public IdentityRole[] Roles { get; private set; }

        public UserCategories(RoleManager<IdentityRole> roleManager, UserManager<SiteUser> userManager)
        {
            this.Status = userManager.Users.Select(user => user.Status).Distinct().ToArray();
            this.Roles = roleManager.Roles.Distinct().ToArray();
        }
    }
}
