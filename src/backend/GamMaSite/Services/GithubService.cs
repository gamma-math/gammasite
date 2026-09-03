using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;
using MimeTypes;

namespace GamMaSite.Services
{
    public class GithubService : IIndexService
    {
        private readonly string _contentApi;
        private readonly string _token;
        private readonly JsonSerializerOptions _options;

        public GithubService(string contentApi, string token)
        {
            this._contentApi = contentApi;
            this._token = token;

            _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<List<ContentMeta>> GetContentMetasAsync(string query)
        {
            return await GetResult<List<ContentMeta>>(query);
        }

        public async Task<ContentType> GetContentAsync(string query)
        {
            var content = await GetResult<GithubContent>(query);
            var mimeType = MimeTypeMap.GetMimeType(content.Name ?? "txt");
            if (new[] { "text/plain", "application/octet-stream" }.Contains(mimeType))
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
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Authorization", $"token {_token}");
            client.DefaultRequestHeaders.Add("User-Agent", "GamMaSite");

            var response = await client.GetAsync($"{_contentApi}{query}");

            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return Activator.CreateInstance<TResult>();
            try
            {
                var result = JsonSerializer.Deserialize<TResult>(responseString, _options);
                return result;
            }
            catch (Exception)
            {
                return Activator.CreateInstance<TResult>();
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
                return !string.IsNullOrEmpty(Content) ? Convert.FromBase64String(Content) : Array.Empty<byte>();
            }
        }
    }
}
