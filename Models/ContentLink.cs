using System;

namespace GamMaSite.Models
{
    public class ContentLink
    {
        public int Id { get; set; }

        public int ContentItemId { get; set; }

        public string Label { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }

        public int SortOrder { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public ContentItem ContentItem { get; set; }
    }
}
