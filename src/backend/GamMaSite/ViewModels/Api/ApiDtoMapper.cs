using System.Linq;
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
            return new ContentItemDto
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Body = item.Body,
                PictureUrl = item.PictureUrl,
                Tags = item.Tags,
                Type = item.Type,
                Status = item.Status,
                MaxParticipants = item.MaxParticipants,
                ShowOnFrontPage = item.ShowOnFrontPage,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Location = item.Location,
                CreatedByUserId = item.CreatedByUserId,
                Created = item.Created,
                Updated = item.Updated,
                PublishedAt = item.PublishedAt,
                Links = item.Links
                    .OrderBy(link => link.SortOrder)
                    .Select(link => link.ToDto())
                    .ToList()
            };
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

        public static PaymentOfferDto ToDto(this PaymentOffer offer)
        {
            var link = offer.ContentItem?.Links?
                .FirstOrDefault(item => item.Type == "PAYMENT" && item.Url.Contains($"/pay/{offer.Id}", System.StringComparison.OrdinalIgnoreCase));

            return new PaymentOfferDto
            {
                Id = offer.Id,
                ContentItemId = offer.ContentItemId,
                Provider = offer.Provider,
                OfferType = offer.OfferType,
                ProductName = offer.ProductName,
                AmountMinor = offer.AmountMinor,
                Currency = offer.Currency,
                IsActive = offer.IsActive,
                ProviderProductId = offer.ProviderProductId,
                ProviderPriceId = offer.ProviderPriceId,
                ProviderPaymentUrl = offer.ProviderPaymentUrl,
                LinkLabel = link?.Label,
                PaymentUrl = link?.Url,
                EventTitle = offer.ContentItem?.Title,
                EventSlug = offer.ContentItem?.Slug
            };
        }

        public static EventRegistrationDto ToDto(this EventRegistration registration, bool includePrivateDetails = false)
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
                ResponseText = registration.ResponseText,
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
