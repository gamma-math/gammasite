using System;
using System.Collections.Generic;
using System.Linq;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress, fromAddress));
            message.To.Add(new MailboxAddress(toAddress, toAddress));
            message.Subject = subject;
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Connect(this.host, this.port, this.enableSSL);

            client.Authenticate(this.userName, this.password);

            await client.SendAsync(message);
            client.Disconnect(true);
        }

        public async Task SendEmailAsync(string[] toAddresses, string subject, string htmlMessage)
        {
            var mailTasks = new List<Task>();
            foreach (var address in toAddresses)
            {
                mailTasks.Add(SendEmailAsync(address, subject, htmlMessage));
            }
            await Task.WhenAll(mailTasks);
        }
    }
}
