using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace GamMaSite.Models
{
    public class RoleEdit
    {
        public IdentityRole Role { get; set; }
        public IEnumerable<SiteUser> Members { get; set; }

        public IEnumerable<SiteUser> NonMembers { get; set; }
    }
}
