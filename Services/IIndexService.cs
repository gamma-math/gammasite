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
            return new string[] { "file", "blob" }.Contains(Type) ? "🗎" : "▣";
        }
    }

    public class ContentType
    {
        public string MimeType { get; set; }
        public byte[] Content { get; set; }
    }
}
