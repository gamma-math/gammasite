using System;

namespace GamMaSite.Models
{
    /*
     * Stores one user's relationship to an event, including role and attendance status.
     */
    public class EventRegistration
    {
        public int Id { get; set; }

        public int ContentItemId { get; set; }

        public string UserId { get; set; }

        public string RegistrationType { get; set; }

        public bool Registered { get; set; }

        public string ResponseText { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public ContentItem ContentItem { get; set; }

        public SiteUser User { get; set; }
    }
}
