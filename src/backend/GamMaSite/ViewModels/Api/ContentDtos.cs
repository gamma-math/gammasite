using System;
using System.Collections.Generic;

namespace GamMaSite.ViewModels.Api
{
    /*
     * DTOs for event/news content, related links, and admin save requests.
     */
    public class ContentLinkDto
    {
        public int Id { get; set; }

        public string Label { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }

        public int SortOrder { get; set; }
    }

    public class ContentItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Slug { get; set; }

        public string Summary { get; set; }

        public string Body { get; set; }

        public string PictureUrl { get; set; }

        public string Tags { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public bool ShowOnFrontPage { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Location { get; set; }

        public string CreatedByUserId { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public DateTime? PublishedAt { get; set; }

        public List<ContentLinkDto> Links { get; set; } = new List<ContentLinkDto>();
    }

    public class SaveContentItemRequest
    {
        public string Title { get; set; }

        public string Slug { get; set; }

        public string Summary { get; set; }

        public string Body { get; set; }

        public string PictureUrl { get; set; }

        public string Tags { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public bool ShowOnFrontPage { get; set; } = true;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Location { get; set; }

        public DateTime? PublishedAt { get; set; }

        public List<SaveContentLinkRequest> Links { get; set; } = new List<SaveContentLinkRequest>();
    }

    public class SaveContentLinkRequest
    {
        public string Label { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }

        public int SortOrder { get; set; }
    }
}
