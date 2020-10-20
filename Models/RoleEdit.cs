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
        public IEnumerable<GamMaUser> Members { get; set; }

        public IEnumerable<GamMaUser> NonMembers { get; set; }
    }
}
