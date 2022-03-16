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
            return new[] { "file", "blob" }.Contains(Type) ? "🗎" : "▣";
        }
        
        public string GetRoot()
        {
            var split = Path.Split("/").ToList();
            var cut = Math.Max(0, split.Count - 1);
            return split.Count > 1 ? $"/{string.Join("/", split.GetRange(0, cut))}" : "/";
        }
        
        public string GetParent()
        {
            var split = Path.Split("/").ToList();
            var cut = Math.Max(0, split.Count - 2);
            return split.Count > 1 ? string.Join("/", split.GetRange(0, cut)) : "/";
        }

        public bool HasParent()
        {
            return Path.Split("/").Length > 1;
        }
    }

    public class ContentType
    {
        public string MimeType { get; set; }
        public byte[] Content { get; set; }
    }
}
