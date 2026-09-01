using System.Linq;
using GamMaSite.Models;

namespace GamMaSite.ViewModels.Api
{
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

        public static EventRegistrationDto ToDto(this EventRegistration registration)
        {
            return new EventRegistrationDto
            {
                Id = registration.Id,
                ContentItemId = registration.ContentItemId,
                UserId = registration.UserId,
                UserName = registration.User?.Navn ?? registration.User?.UserName,
                Email = registration.User?.Email,
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
