using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace GamMaSite.Services
{
    public interface IEmailService : IEmailSender
    {
        public Task SendEmailAsync(IEnumerable<string> emails, string subject, string htmlMessage);
        
    }
}
