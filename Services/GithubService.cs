using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public class GithubService : IIndexService
    {
        private string contentAPI;
        private string token;
        private JsonSerializerOptions options;

        public GithubService(string contentAPI, string token)
        {
            this.contentAPI = contentAPI;
            this.token = token;

            this.options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<ContentMeta>> GetContentMetasAsync(string query)
        {
            return await GetResult<List<ContentMeta>>(query);
        }

        public async Task<ContentType> GetContentAsync(string query)
        {
            return await GetResult<ContentType>(query);
        }

        public async Task<byte[]> GetContentBytesAsync(string query)
        {
            var contentBytes = (await GetResult<ContentType>(query)).ContentBytes();
            return contentBytes;
        }

        private async Task<TResult> GetResult<TResult>(string query)
        {
            var handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"token {this.token}");
                client.DefaultRequestHeaders.Add("User-Agent", "GamMaSite");

                var response = await client.GetAsync($"{this.contentAPI}{query}");

                var responseString = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TResult>(responseString, this.options);
                    return result;
                }
                else
                {
                    var result = Activator.CreateInstance<TResult>();
                    return result;
                }
            }
        }
    }
}
