using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Models
{
    /*
     * Membership status values used for member filtering, payment state, and admin updates.
     */
    public enum UserStatus
    {
        OPRETTET = 0,
        BETALT = 1,
        SKYLDER = 2,
        INAKTIV = 3,
        STUDERENDE = 4
    }
}
