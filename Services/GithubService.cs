using MimeTypes;
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
            var content = await GetResult<GithubContent>(query);
            var mimeType = MimeTypeMap.GetMimeType(content.Name != null ? content.Name : "txt");
            if (new string[] { "text/plain", "application/octet-stream" }.Contains(mimeType))
            {
                mimeType = "text/plain;charset=utf-8";
            }
            return new ContentType
            {
                Content = content.ContentBytes(),
                MimeType = mimeType
            };
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
                    try
                    {
                        var result = JsonSerializer.Deserialize<TResult>(responseString, this.options);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        return Activator.CreateInstance<TResult>();
                    }
                }
                else
                {
                    return Activator.CreateInstance<TResult>();
                }
            }
        }

        private class GithubContent
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Sha { get; set; }
            public string Type { get; set; }
            public string Content { get; set; }
            public byte[] ContentBytes()
            {
                return !string.IsNullOrEmpty(this.Content) ? Convert.FromBase64String(this.Content) : new byte[0];
            }
        }
    }
}
