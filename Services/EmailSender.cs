using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public class EmailSender : IEmailSender
    {
        // Our private configuration variables
        private string host;
        private int port;
        private bool enableSSL;
        private string userName;
        private string password;
        private string fromAddress;

        // Get our parameterized configuration
        public EmailSender(string host, int port, bool enableSSL, string userName, string password)
        {
            this.host = host;
            this.port = port;
            this.enableSSL = enableSSL;
            this.userName = userName;
            this.password = password;
            this.fromAddress = userName;
        }

        public EmailSender(string host, int port, bool enableSSL, string userName, string password, string fromAddress)
        {
            this.host = host;
            this.port = port;
            this.enableSSL = enableSSL;
            this.userName = userName;
            this.password = password;
            this.fromAddress = fromAddress;
        }

        public async Task SendEmailAsync(string toAddress, string subject, string htmlMessage)
        {
            string[] mails = toAddress.Split(";");
            using (var client = new SmtpClient(host, port) {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = enableSSL
            })
            {
                foreach (var email in mails)
                {
                    var mailMessage = new MailMessage(fromAddress, email, subject, htmlMessage) { IsBodyHtml = true };
                    await client.SendMailAsync(mailMessage);
                }
            }
        }
    }
}
