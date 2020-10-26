﻿using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamMaSite.Models
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
}
