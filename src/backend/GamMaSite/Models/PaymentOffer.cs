using System;

namespace GamMaSite.Models
{
    public static class PaymentOfferProviders
    {
        public const string Stripe = "STRIPE";
        public const string MobilePay = "MOBILEPAY";
    }

    public static class PaymentOfferScopes
    {
        public const string Event = "EVENT";
        public const string Membership = "MEMBERSHIP";
    }

    public static class PaymentOfferTypes
    {
        public const string OneTime = "ONE_TIME";
        public const string Recurring = "RECURRING";
    }

    /*
     * Stores a payment offer for either an event or the global membership flow.
     * Provider-specific identifiers stay here; events and users remain provider
     * independent.
     */
    public class PaymentOffer
    {
        public int Id { get; set; }

        public int? ContentItemId { get; set; }

        // Stored as a generated database value so NULL global offers participate
        // in the unique scope/provider/type constraint.
        public int ContentItemScopeKey { get; private set; }

        public string Scope { get; set; }

        public string Provider { get; set; }

        public string OfferType { get; set; }

        public string LinkLabel { get; set; }

        public string ProductName { get; set; }

        public string ProviderProductId { get; set; }

        public string ProviderPriceId { get; set; }

        public string ProviderPaymentLinkId { get; set; }

        public string ProviderPaymentUrl { get; set; }

        public long? AmountMinor { get; set; }

        public string Currency { get; set; }

        public string BillingInterval { get; set; }

        public int? BillingIntervalCount { get; set; }

        public bool IsActive { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public ContentItem ContentItem { get; set; }
    }
}
