using GamMaSite.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamMaSite.Areas.Identity.Data
{
    public class GamMaUser : IdentityUser
    {
        [PersonalData]
        public string Navn { get; set; }

        [PersonalData]
        public string Adresse { get; set; }

        [PersonalData]
        public int Aargang { get; set; }

        [PersonalData]
        public string Beskaeftigelse { get; set; }

        [PersonalData]
        public UserStatus Status { get; set; }

        [PersonalData]
        public DateTime KontingentDato { get; set; }

    }
}
