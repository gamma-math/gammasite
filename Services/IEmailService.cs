using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IEmailService
    {
        public Task SendEmailAsync(string[] emails, string subject, string htmlMessage);
        
        public Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
