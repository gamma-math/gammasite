using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public class SmsSender : ISmsSender
    {
        private string host;
        private string apiKey;
        private string from;
        private JsonSerializerOptions options;

        // Get our parameterized configuration
        public SmsSender(string host, string apiKey, string from)
        {
            this.host = host;
            this.apiKey = apiKey;
            this.from = from;

            this.options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<string> SendSmsAsync(string message, params string[] recipients)
        {
            var request = GetRequest(message, recipients);
            var json = JsonSerializer.Serialize(request, this.options);
            var handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };

            using (var client = new HttpClient(handler))
            {
                var base64 = Convert.ToBase64String(Encoding.Default.GetBytes($"{this.apiKey}:"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {base64}");

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(this.host, httpContent);

                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
        }

        private GatewayRequest GetRequest(string message, params string[] recipients)
        {
            List<Recipient> recipientList = new List<Recipient>();
            recipients.ToList().ForEach(rec => {
                recipientList.Add(new Recipient(FormatPhoneNumber(rec)));
            });

            var request = new GatewayRequest
            {
                Message = message,
                Sender = this.from,
                Recipients = recipientList
            };
            return request;
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            var formattedNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("+","");
            return formattedNumber.Length == 8 ? $"45{formattedNumber}" : formattedNumber;
        }
    }

    public class GatewayRequest
    {
        public string Message { get; set; }
        public string Sender { get; set; }
        public List<Recipient> Recipients { get; set; }
    }

    public class Recipient
    {
        public string Msisdn { get; set; }

        public Recipient(string msisdn)
        {
            this.Msisdn = msisdn;
        }
    }
}
