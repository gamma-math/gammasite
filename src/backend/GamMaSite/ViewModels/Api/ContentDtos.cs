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

        public int? MaxParticipants { get; set; }

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

        public int? MaxParticipants { get; set; }

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

    public class PaymentOfferDto
    {
        public int Id { get; set; }

        public int ContentItemId { get; set; }

        public string Provider { get; set; }

        public string OfferType { get; set; }

        public string ProductName { get; set; }

        public long? AmountMinor { get; set; }

        public string Currency { get; set; }

        public bool IsActive { get; set; }

        public string ProviderProductId { get; set; }

        public string ProviderPriceId { get; set; }

        public string LinkLabel { get; set; }

        public string PaymentUrl { get; set; }

        public string ProviderPaymentUrl { get; set; }

        public string EventTitle { get; set; }

        public string EventSlug { get; set; }
    }

    public class SavePaymentOfferRequest
    {
        public string LinkLabel { get; set; }

        public string Provider { get; set; }

        public decimal Amount { get; set; }

        public string ProductName { get; set; }
    }
}
