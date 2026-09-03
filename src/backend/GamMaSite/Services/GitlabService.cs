using MimeTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace GamMaSite.Services
{
    public class GitlabService : IIndexService
    {
        private readonly string _contentApi;
        private readonly string _token;
        private readonly JsonSerializerOptions _options;

        public GitlabService(string contentApi, string token)
        {
            this._contentApi = contentApi;
            this._token = token;

            this._options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<List<ContentMeta>> GetContentMetasAsync(string path)
        {
            var query = $"tree?path={HttpUtility.UrlEncode(path)}";
            return await GetResult<List<ContentMeta>>(query);
        }

        public async Task<ContentType> GetContentAsync(string path)
        {
            var query = $"files/{HttpUtility.UrlEncode(path)}?ref=master";
            var content = await GetResult<GitlabContent>(query);
            var mimeType = MimeTypeMap.GetMimeType(content.File_Name ?? "txt");
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
            client.DefaultRequestHeaders.Add("Private-Token", $"{this._token}");
            client.DefaultRequestHeaders.Add("User-Agent", "GamMaSite");

            var response = await client.GetAsync($"{this._contentApi}{query}");

            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return Activator.CreateInstance<TResult>();
            try
            {
                var result = JsonSerializer.Deserialize<TResult>(responseString, this._options);
                return result;
            }
            catch (Exception)
            {
                return Activator.CreateInstance<TResult>();
            }
        }

        private class GitlabContent
        {
            public string File_Name { get; set; }
            public string File_Path { get; set; }
            public string Content { get; set; }
            public byte[] ContentBytes()
            {
                return !string.IsNullOrEmpty(this.Content) ? Convert.FromBase64String(this.Content) : Array.Empty<byte>();
            }
        }
    }
}
