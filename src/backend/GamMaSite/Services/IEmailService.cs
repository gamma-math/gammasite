using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace GamMaSite.Services
{
    /*
     * Defines email sending methods shared by Identity and application messaging.
     */
    public interface IEmailService : IEmailSender
    {
        public Task SendEmailAsync(IEnumerable<string> emails, string subject, string htmlMessage);
        
    }
}
