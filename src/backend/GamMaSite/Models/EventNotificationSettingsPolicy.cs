using System;
using System.Collections.Generic;

namespace GamMaSite.Models
{
    /*
     * Defines the supported event notification modes and their mail behavior.
     */
    public static class EventNotificationSettingsPolicy
    {
        public const string Default = "DEFAULT";
        public const string OnlyWhenNoPaymentOffer = "OnlyWhenNoPaymentOffer";
        public const string Never = "NEVER";

        private static readonly HashSet<string> AllowedValues = new(StringComparer.OrdinalIgnoreCase)
        {
            Default,
            OnlyWhenNoPaymentOffer,
            Never
        };

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (!AllowedValues.Contains(normalized))
            {
                throw new ArgumentException($"Unsupported event notification settings: {value}");
            }

            if (string.Equals(normalized, Default, StringComparison.OrdinalIgnoreCase))
            {
                return Default;
            }

            if (string.Equals(normalized, OnlyWhenNoPaymentOffer, StringComparison.OrdinalIgnoreCase))
            {
                return OnlyWhenNoPaymentOffer;
            }

            return Never;
        }

        public static bool ShouldSendRegistrationConfirmation(string value)
        {
            var effective = NormalizeOrDefault(value);
            return effective == Default;
        }

        public static bool ShouldSendPaymentConfirmation(string paymentOfferScope, string value)
        {
            if (!string.Equals(paymentOfferScope, PaymentOfferScopes.Event, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return NormalizeOrDefault(value) != Never;
        }

        public static bool ShouldSendWaitlistPromotion(string value)
        {
            return NormalizeOrDefault(value) != Never;
        }

        private static string NormalizeOrDefault(string value)
        {
            try
            {
                return Normalize(value) ?? Default;
            }
            catch (ArgumentException)
            {
                // Invalid legacy values should keep the existing send behavior.
                return Default;
            }
        }
    }
}
