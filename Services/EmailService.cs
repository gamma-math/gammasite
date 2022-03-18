using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;

namespace GamMaSite.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly string _fromAddress;
        private readonly string _apiKey;

        public EmailService(string host, string fromAddress, string apiKey)
        {
            this._host = host;
            this._fromAddress = fromAddress;
            this._apiKey = apiKey;
        }

        public async Task SendEmailAsync(IEnumerable<string> emails, string subject, string htmlMessage)
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
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{this._apiKey}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {authToken}");

            IList<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>> {
                new("from", this._fromAddress),
                new("to", toAddress),
                new("subject", subject),
                new("html", htmlMessage ?? "<p></p>")
            };

            await client.PostAsync(this._host, new FormUrlEncodedContent(keyValues));
        }
    }
}
