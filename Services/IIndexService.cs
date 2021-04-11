using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IIndexService
    {
        public Task<List<ContentMeta>> GetContentMetasAsync(string query);

        public Task<ContentType> GetContentAsync(string query);
    }

    public class ContentMeta
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }

        public string TypeConverted()
        {
            return Type == "file" ? "🗎" : "▣";
        }
    }

    public class ContentType
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
