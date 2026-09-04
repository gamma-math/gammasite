using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace GamMaSite.Models
{
    /*
     * Message channel values used by admin email/SMS sending flows.
     */
    public enum MessageMedia
    {
        Email,
        SMS,
        EmailSMS
    }
}
