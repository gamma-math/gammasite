using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace GamMaSite.Models
{
    /*
     * Legacy MVC post model for adding and removing users from a role.
     */
    public class RoleModification
    {
        [Required(ErrorMessage = "{0} er obligatorisk")]
        public string RoleName { get; set; }

        public string RoleId { get; set; }

        public string[] AddIds { get; set; }

        public string[] DeleteIds { get; set; }
    }
}
