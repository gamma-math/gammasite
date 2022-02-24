using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;

namespace GamMaSite.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string host;
        private readonly string fromAddress;
        private readonly string apiKey;

        public EmailSender(string host, string fromAddress, string apiKey)
        {
            this.host = host;
            this.fromAddress = fromAddress;
            this.apiKey = apiKey;
        }

        public async Task SendEmailAsync(string[] toAddresses, string subject, string htmlMessage)
        {
            var mailTasks = new List<Task>();
            foreach (var address in toAddresses)
            {
                mailTasks.Add(PostMailgun(address, subject, htmlMessage));
            }
            await Task.WhenAll(mailTasks);
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
                { new KeyValuePair<string, string>("from", this.fromAddress) },
                { new KeyValuePair<string, string>("to", toAddress) },
                { new KeyValuePair<string, string>("subject", subject) },
                { new KeyValuePair<string, string>("html", htmlMessage) }
            };

            await client.PostAsync(this.host, new FormUrlEncodedContent(keyValues));
        }
    }
}
