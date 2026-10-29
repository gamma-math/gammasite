using System;
using GamMaSite.Models;

namespace GamMaSite.Services
{
    /*
     * Centralizes the public event-registration and payment-open rules.
     */
    public static class EventRegistrationRules
    {
        public static DateTime? GetRegistrationDeadline(ContentItem content)
        {
            return content?.RegistrationDeadline ?? content?.EndDate ?? content?.StartDate;
        }

        public static bool IsRegistrationOpen(ContentItem content, DateTime utcNow)
        {
            return content != null
                && string.Equals(content.Type, ContentTypes.Event, StringComparison.OrdinalIgnoreCase)
                && string.Equals(content.Status, ContentStatuses.Published, StringComparison.OrdinalIgnoreCase)
                && GetRegistrationDeadline(content).GetValueOrDefault() > utcNow;
        }

        public static bool IsEventEnded(ContentItem content, DateTime utcNow)
        {
            var eventEnd = content?.EndDate ?? content?.StartDate;
            return !eventEnd.HasValue || eventEnd.Value <= utcNow;
        }

        public static bool IsUnpaidParticipant(EventRegistration registration)
        {
            return registration != null
                && !registration.Registered
                && (string.Equals(registration.RegistrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(registration.RegistrationType, RegistrationTypes.Organizer, StringComparison.OrdinalIgnoreCase));
        }

        public static bool CanAccessEventPayment(ContentItem content, EventRegistration registration, DateTime utcNow)
        {
            return IsRegistrationOpen(content, utcNow)
                || (!IsEventEnded(content, utcNow) && IsUnpaidParticipant(registration));
        }
    }
}
