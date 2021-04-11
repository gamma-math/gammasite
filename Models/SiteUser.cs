using GamMaSite.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GamMaSite.Models
{
    public class SiteUser : IdentityUser
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
        public VisibilityStatus Visibility { get; set; }

        [PersonalData]
        public DateTime KontingentDato { get; set; }

        [PersonalData]
        public DateTime OprettetDato { get; set; }

        public void MarkAsPayed()
        {
            if (this.Status != UserStatus.BETALT)
            {
                this.Status = UserStatus.BETALT;
                this.KontingentDato = DateTime.Now;
            }
        }
    }

    public class Metadata
    {
        public override string ToString()
        {
            return this != null ? JsonSerializer.Serialize<Metadata>(this) : null;
        }
    }
}
