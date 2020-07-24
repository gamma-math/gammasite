using GamMaSite.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;

namespace GamMaSite.Models
{
    public class GamMaUser : IdentityUser
    {
        [PersonalData]
        public string Navn { get; set; }

        [PersonalData]
        public string Adresse { get; set; }

        [PersonalData]
        public string Aargang { get; set; }

        [PersonalData]
        public UserStatus Status { get; set; }

    }
}
