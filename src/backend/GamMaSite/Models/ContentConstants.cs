namespace GamMaSite.Models
{
    /*
     * Canonical content type values stored in the database and used by React.
     */
    public static class ContentTypes
    {
        public const string News = "NEWS";
        public const string Event = "EVENT";
    }

    /*
     * Canonical publication status values for content items.
     */
    public static class ContentStatuses
    {
        public const string Draft = "DRAFT";
        public const string Published = "PUBLISHED";
        public const string Archived = "ARCHIVED";
    }

    /*
     * Canonical event registration type values used by attendee management.
     */
    public static class RegistrationTypes
    {
        public const string Attendee = "ATTENDEE";
        public const string Organizer = "ORGANIZER";
        public const string Interested = "INTERESTED";
        public const string Declined = "DECLINED";
    }
}
