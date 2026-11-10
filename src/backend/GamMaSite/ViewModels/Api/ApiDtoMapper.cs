using System.Linq;
using System;
using GamMaSite.Models;

namespace GamMaSite.ViewModels.Api
{
    /*
     * Maps EF/domain models into DTOs returned by React API endpoints.
     */
    public static class ApiDtoMapper
    {
        public static ContentItemDto ToDto(this ContentItem item)
        {
            return MapContentItem(item, new ContentItemDto());
        }

        public static AdminContentItemDto ToAdminDto(this ContentItem item)
        {
            var dto = MapContentItem(item, new AdminContentItemDto());
            dto.EventNotificationSettings = item.EventNotificationSettings;
            return dto;
        }

        private static T MapContentItem<T>(ContentItem item, T dto) where T : ContentItemDto
        {
            dto.Id = item.Id;
            dto.Title = item.Title;
            dto.Slug = item.Slug;
            dto.Summary = item.Summary;
            dto.Body = item.Body;
            dto.PictureUrl = item.PictureUrl;
            dto.Tags = item.Tags;
            dto.Type = item.Type;
            dto.Status = item.Status;
            dto.MaxParticipants = item.MaxParticipants;
            dto.ShowOnFrontPage = item.ShowOnFrontPage;
            dto.StartDate = item.StartDate;
            dto.EndDate = item.EndDate;
            dto.RegistrationDeadline = item.RegistrationDeadline;
            dto.Location = item.Location;
            dto.CreatedByUserId = item.CreatedByUserId;
            dto.Created = item.Created;
            dto.Updated = item.Updated;
            dto.PublishedAt = item.PublishedAt;
            dto.Links = item.Links
                .Where(link => !IsManagedPaymentLink(link))
                .OrderBy(link => link.SortOrder)
                .Select(link => link.ToDto())
                .ToList();
            dto.PaymentOffers = item.PaymentOffers
                .Where(offer => offer.IsActive)
                .OrderBy(offer => offer.Created)
                .ThenBy(offer => offer.Id)
                .Select(offer => offer.ToDto(item))
                .ToList();
            return dto;
        }

        private static bool IsManagedPaymentLink(ContentLink link)
        {
            return string.Equals(link.Type, "PAYMENT", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(link.Url, UriKind.RelativeOrAbsolute, out var uri)
                && System.Text.RegularExpressions.Regex.IsMatch(
                    uri.IsAbsoluteUri ? uri.AbsolutePath : link.Url,
                    @"/(?:react/)?events/[^/]+/pay/\d+$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public static ContentLinkDto ToDto(this ContentLink link)
        {
            return new ContentLinkDto
            {
                Id = link.Id,
                Label = link.Label,
                Url = link.Url,
                Type = link.Type,
                SortOrder = link.SortOrder
            };
        }

        public static PaymentOfferDto ToDto(this PaymentOffer offer, ContentItem contentItem = null)
        {
            var owner = contentItem ?? offer.ContentItem;
            var paymentUrl = owner?.Slug == null
                ? null
                : $"/events/{Uri.EscapeDataString(owner.Slug)}/pay/{offer.Id}";
            var legacyLabel = owner?.Links?
                .FirstOrDefault(link => IsManagedPaymentLink(link) && link.Url?.EndsWith($"/pay/{offer.Id}", StringComparison.OrdinalIgnoreCase) == true)?
                .Label;

            return new PaymentOfferDto
            {
                Id = offer.Id,
                ContentItemId = offer.ContentItemId,
                Scope = offer.Scope,
                Provider = offer.Provider,
                OfferType = offer.OfferType,
                LinkLabel = offer.LinkLabel ?? legacyLabel,
                ProductName = offer.ProductName,
                AmountMinor = offer.AmountMinor,
                Currency = offer.Currency,
                BillingInterval = offer.BillingInterval,
                BillingIntervalCount = offer.BillingIntervalCount,
                IsActive = offer.IsActive,
                ProviderProductId = offer.ProviderProductId,
                ProviderPriceId = offer.ProviderPriceId,
                ProviderPaymentUrl = offer.ProviderPaymentUrl,
                PaymentUrl = paymentUrl,
                EventTitle = owner?.Title,
                EventSlug = owner?.Slug
            };
        }

        public static EventRegistrationDto ToDto(this EventRegistration registration, bool includePrivateDetails = false, bool includeResponseText = false)
        {
            return new EventRegistrationDto
            {
                Id = registration.Id,
                ContentItemId = registration.ContentItemId,
                UserId = registration.UserId,
                UserName = registration.User?.Navn ?? registration.User?.UserName,
                Email = includePrivateDetails || registration.User?.Visibility.IsVisible() == true ? registration.User?.Email : null,
                RegistrationType = registration.RegistrationType,
                Registered = registration.Registered,
                ResponseText = includeResponseText ? registration.ResponseText : null,
                Created = registration.Created,
                Updated = registration.Updated
            };
        }

        public static EmailTemplateDto ToDto(this EmailTemplate template)
        {
            return new EmailTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Subject = template.Subject,
                Preheader = template.Preheader,
                HtmlBody = template.HtmlBody,
                TextBody = template.TextBody,
                TemplateType = template.TemplateType,
                IsActive = template.IsActive,
                Created = template.Created,
                Updated = template.Updated
            };
        }
    }
}
