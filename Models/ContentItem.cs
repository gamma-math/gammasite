using System;
using System.Collections.Generic;

namespace GamMaSite.Models
{
    public class ContentItem
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

        public bool ShowOnFrontPage { get; set; } = true;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Location { get; set; }

        public string CreatedByUserId { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public DateTime? PublishedAt { get; set; }

        public SiteUser CreatedByUser { get; set; }

        public ICollection<ContentLink> Links { get; set; } = new List<ContentLink>();

        public ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
    }
}
