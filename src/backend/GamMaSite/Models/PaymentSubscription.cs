using System;

namespace GamMaSite.Models
{
    public static class PaymentSubscriptionStatuses
    {
        public const string Pending = "pending";
        public const string Active = "active";
        public const string Trialing = "trialing";
        public const string PastDue = "past_due";
        public const string Incomplete = "incomplete";
        public const string Canceled = "canceled";
        public const string Expired = "expired";
    }

    /*
     * Stores the current subscription state for a user. Payment history remains
     * outside this table; provider webhooks update this current-state record.
     */
    public class PaymentSubscription
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int? PaymentOfferId { get; set; }

        public PaymentOffer PaymentOffer { get; set; }

        public string Provider { get; set; }

        public string ProviderSubscriptionId { get; set; }

        public string ProductName { get; set; }

        public string ProviderProductId { get; set; }

        public string ProviderPriceId { get; set; }

        public string Status { get; set; }

        public DateTime? CurrentPeriodStart { get; set; }

        public DateTime? CurrentPeriodEnd { get; set; }

        public DateTime? NextChargeAt { get; set; }

        public DateTime? LastPaidAt { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public string LastProcessedWebhookId { get; set; }

        public DateTime? LastProcessedWebhookAt { get; set; }
    }
}
