using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace GamMaSite.Services
{
    public class EmailService : IEmailService, IEmailSender
    {
        private readonly string host;
        private readonly string fromAddress;
        private readonly string apiKey;

        public EmailService(string host, string fromAddress, string apiKey)
        {
            this.host = host;
            this.fromAddress = fromAddress;
            this.apiKey = apiKey;
        }

        public async Task SendEmailAsync(string[] emails, string subject, string htmlMessage)
        {
            var mailTasks = emails.Select(address => PostMailgun(address, subject, htmlMessage)).ToList();

            await Task.WhenAll(mailTasks);
        }
        
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await PostMailgun(email, subject, htmlMessage);
        }

        private async Task PostMailgun(string toAddress, string subject, string htmlMessage)
        {
            var handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };
            using var client = new HttpClient(handler);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{this.apiKey}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {authToken}");

            IList<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>> {
                new("from", this.fromAddress),
                new("to", toAddress),
                new("subject", subject),
                new("html", htmlMessage)
            };

            await client.PostAsync(this.host, new FormUrlEncodedContent(keyValues));
        }
    }
}
