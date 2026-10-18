using System;

namespace GamMaSite.Models
{
    public static class PaymentOfferProviders
    {
        public const string Stripe = "STRIPE";
        public const string MobilePay = "MOBILEPAY";
    }

    public static class PaymentOfferTypes
    {
        public const string OneTime = "ONE_TIME";
    }

    /*
     * Stores the current payment offer attached to an event. Provider-specific
     * identifiers stay here; events and users remain provider independent.
     */
    public class PaymentOffer
    {
        public int Id { get; set; }

        public int ContentItemId { get; set; }

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

        public bool IsActive { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public ContentItem ContentItem { get; set; }
    }
}
