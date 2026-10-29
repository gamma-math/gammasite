using System;

namespace GamMaSite.ViewModels.Api
{
    /*
     * DTOs for event registration display, signup, and admin attendee updates.
     */
    public class EventRegistrationDto
    {
        public int Id { get; set; }

        public int ContentItemId { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string RegistrationType { get; set; }

        public bool Registered { get; set; }

        public string ResponseText { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }

    public class EventRegistrationSummaryDto
    {
        public int ContentItemId { get; set; }

        public int? MaxParticipants { get; set; }

        public int ConfirmedParticipants { get; set; }

        public int WaitlistedParticipants { get; set; }

        public bool IsFull { get; set; }
    }

    public class AccountEventRegistrationDto
    {
        public int EventId { get; set; }

        public string Title { get; set; }

        public string Slug { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string RegistrationType { get; set; }

        public bool Registered { get; set; }
    }

    public class SaveEventRegistrationRequest
    {
        public string RegistrationType { get; set; }

        public string ResponseText { get; set; }
    }

    public class UpdateEventRegistrationRequest
    {
        public string RegistrationType { get; set; }

        public bool Registered { get; set; }

        public string ResponseText { get; set; }
    }

    public class AddEventRegistrationRequest
    {
        public string UserId { get; set; }

        public string RegistrationType { get; set; }

        public bool Registered { get; set; }

        public string ResponseText { get; set; }
    }
}
