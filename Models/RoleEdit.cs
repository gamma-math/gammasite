using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using GamMaSite.Areas.Identity.Data;

namespace GamMaSite.Models
{
    public class RoleEdit
    {
        public IdentityRole Role { get; set; }
        public IEnumerable<GamMaUser> Members { get; set; }

        public IEnumerable<GamMaUser> NonMembers { get; set; }
    }
}
