using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IEmailSender
    {
        public Task SendEmailAsync(string[] toAddresses, string subject, string htmlMessage);

    }
}
