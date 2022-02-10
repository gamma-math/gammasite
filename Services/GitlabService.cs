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
        private string contentAPI;
        private string token;
        private JsonSerializerOptions options;

        public GitlabService(string contentAPI, string token)
        {
            this.contentAPI = contentAPI;
            this.token = token;

            this.options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
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
            var mimeType = MimeTypeMap.GetMimeType(content.File_Name != null ? content.File_Name : "txt");
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
                client.DefaultRequestHeaders.Add("Private-Token", $"{this.token}");
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
                    catch (Exception)
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

        private class GitlabContent
        {
            public string File_Name { get; set; }
            public string File_Path { get; set; }
            public string Content { get; set; }
            public byte[] ContentBytes()
            {
                return !string.IsNullOrEmpty(this.Content) ? Convert.FromBase64String(this.Content) : new byte[0];
            }
        }
    }
}
