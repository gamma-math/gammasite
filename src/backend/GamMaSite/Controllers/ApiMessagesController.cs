using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize(Roles = "Admin,ADMIN")]
    [AutoValidateAntiforgeryToken]
    public class ApiMessagesController : ControllerBase
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\{([A-Za-z0-9_]+)\}\}", RegexOptions.Compiled);

        private readonly ApplicationDbContext _db;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<SiteUser> _userManager;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IEmailService _emailService;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<ApiMessagesController> _logger;

        public ApiMessagesController(
            ApplicationDbContext db,
            RoleManager<IdentityRole> roleManager,
            UserManager<SiteUser> userManager,
            IEmailTemplateService emailTemplateService,
            IEmailService emailService,
            ISmsSender smsSender,
            ILogger<ApiMessagesController> logger)
        {
            _db = db;
            _roleManager = roleManager;
            _userManager = userManager;
            _emailTemplateService = emailTemplateService;
            _emailService = emailService;
            _smsSender = smsSender;
            _logger = logger;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var statuses = await _userManager.Users
                .Select(user => user.Status)
                .Distinct()
                .ToListAsync();
            var roles = await _roleManager.Roles
                .OrderBy(role => role.Name)
                .Select(role => role.Name)
                .ToListAsync();

            return Ok(new MessageCategoriesDto
            {
                Statuses = statuses.Select(status => status.ToString()).OrderBy(status => status).ToList(),
                Roles = roles
            });
        }

        [HttpPost("recipient-preview")]
        public async Task<IActionResult> PreviewRecipients(MessageRecipientPreviewRequest request)
        {
            var recipients = await ResolveRecipients(request?.Statuses, request?.Roles, request?.RecipientEventIds);

            return Ok(new MessageRecipientPreviewDto
            {
                RecipientCount = recipients.Count,
                EmailCount = recipients.Count(user => !string.IsNullOrWhiteSpace(user.Email)),
                SmsCount = recipients.Count(user => !string.IsNullOrWhiteSpace(user.PhoneNumber)),
                Recipients = ToRecipientDtos(recipients)
            });
        }

        [HttpPost("render")]
        public async Task<IActionResult> Render(RenderEmailMessageRequest request)
        {
            try
            {
                if (request == null || request.TemplateId <= 0)
                {
                    return BadRequest(new { error = "Vælg en template først." });
                }

                var template = await _emailTemplateService.GetByIdAsync(request.TemplateId);
                if (template == null)
                {
                    return NotFound(new { error = "Templaten blev ikke fundet." });
                }

                var subjectTemplate = string.IsNullOrWhiteSpace(request.Subject) ? template.Subject : request.Subject;
                var htmlTemplate = string.IsNullOrWhiteSpace(request.BodyOverride) ? template.HtmlBody : request.BodyOverride;
                var blockDesign = EmailBlockDesign.Parse(htmlTemplate);
                htmlTemplate = EmailBlockDesign.RemoveMetadata(htmlTemplate);
                var content = await GetSelectedContent(request.SelectedEventIds, request.SelectedNewsIds);
                var values = BuildTemplateValues(content, blockDesign, GetPublicBaseUrl());
                var hasContentPlaceholder = htmlTemplate.Contains("{{ContentBlocks}}", StringComparison.OrdinalIgnoreCase)
                    || htmlTemplate.Contains("{{EventBlocks}}", StringComparison.OrdinalIgnoreCase)
                    || htmlTemplate.Contains("{{NewsBlocks}}", StringComparison.OrdinalIgnoreCase);
                var html = RenderTemplate(htmlTemplate, values);

                if (!hasContentPlaceholder && !string.IsNullOrWhiteSpace(values["ContentBlocks"]))
                {
                    html = $"{html}{values["ContentBlocks"]}";
                }

                return Ok(new RenderEmailMessageDto
                {
                    Subject = RenderTemplate(subjectTemplate, values),
                    Html = html
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render email message template {TemplateId}", request?.TemplateId);
                return StatusCode(500, new { error = "Email-udkastet kunne ikke genereres på serveren. Tjek serverloggen for detaljer." });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send(SendEmailMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Subject) || string.IsNullOrWhiteSpace(request.Html))
            {
                return BadRequest(new { error = "Emne og indhold er obligatorisk" });
            }

            var recipients = await ResolveRecipients(request.Statuses, request.Roles, request.RecipientEventIds);
            var channel = NormalizeChannel(request.Channel);
            var emailCount = recipients.Count(user => !string.IsNullOrWhiteSpace(user.Email));
            var smsCount = recipients.Count(user => !string.IsNullOrWhiteSpace(user.PhoneNumber));

            if (recipients.Count == 0)
            {
                return BadRequest(new { error = "Der er ingen modtagere i det valgte udsnit." });
            }

            if ((channel == MessageMedia.Email || channel == MessageMedia.EmailSMS) && emailCount == 0)
            {
                return BadRequest(new { error = "Der er ingen valgte modtagere med emailadresse." });
            }

            if ((channel == MessageMedia.SMS || channel == MessageMedia.EmailSMS) && smsCount == 0)
            {
                return BadRequest(new { error = "Der er ingen valgte modtagere med telefonnummer." });
            }

            try
            {
                if (channel == MessageMedia.Email || channel == MessageMedia.EmailSMS)
                {
                    var emails = recipients
                        .Where(user => !string.IsNullOrWhiteSpace(user.Email))
                        .Select(user => user.Email)
                        .Distinct()
                        .ToArray();
                    await _emailService.SendEmailAsync(emails, request.Subject, request.Html);
                }

                if (channel == MessageMedia.SMS || channel == MessageMedia.EmailSMS)
                {
                    var phones = recipients
                        .Where(user => !string.IsNullOrWhiteSpace(user.PhoneNumber))
                        .Select(user => user.PhoneNumber)
                        .Distinct()
                        .ToArray();
                    await _smsSender.SendSmsAsync(string.IsNullOrWhiteSpace(request.SmsBody) ? request.Subject : request.SmsBody, phones);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { error = ex.Message });
            }

            return Ok(new SendEmailMessageResult
            {
                RecipientCount = recipients.Count,
                EmailCount = emailCount,
                SmsCount = smsCount,
                Recipients = ToRecipientDtos(recipients)
            });
        }

        private async Task<HashSet<SiteUser>> ResolveRecipients(string[] statuses, string[] roles, int[] recipientEventIds)
        {
            var recipients = new Dictionary<string, SiteUser>();
            var requestedStatuses = (statuses ?? Array.Empty<string>())
                .Select(status => Enum.TryParse<UserStatus>(status, true, out var parsed) ? parsed : (UserStatus?)null)
                .Where(status => status.HasValue)
                .Select(status => status.Value)
                .ToHashSet();

            if (requestedStatuses.Count > 0)
            {
                AddRecipients(recipients, await _userManager.Users.Where(user => requestedStatuses.Contains(user.Status)).ToListAsync());
            }

            foreach (var role in roles ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    AddRecipients(recipients, await _userManager.GetUsersInRoleAsync(role));
                }
            }

            var eventIds = (recipientEventIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray();
            if (eventIds.Length > 0)
            {
                var eventUserIds = await _db.EventRegistrations
                    .AsNoTracking()
                    .Where(registration => eventIds.Contains(registration.ContentItemId) && registration.Registered)
                    .Select(registration => registration.UserId)
                    .Distinct()
                    .ToListAsync();

                var eventRecipients = await _userManager.Users
                    .AsNoTracking()
                    .Where(user => eventUserIds.Contains(user.Id))
                    .ToListAsync();
                AddRecipients(recipients, eventRecipients);
            }

            return recipients.Values.ToHashSet();
        }

        private static void AddRecipients(IDictionary<string, SiteUser> recipients, IEnumerable<SiteUser> users)
        {
            foreach (var user in users ?? Enumerable.Empty<SiteUser>())
            {
                if (!string.IsNullOrWhiteSpace(user?.Id))
                {
                    recipients[user.Id] = user;
                }
            }
        }

        private static IReadOnlyList<MessageRecipientDto> ToRecipientDtos(IEnumerable<SiteUser> recipients)
        {
            return recipients
                .GroupBy(user => string.IsNullOrWhiteSpace(user.Email) ? user.Id : user.Email.ToLowerInvariant())
                .Select(group => group.First())
                .OrderBy(user => user.Navn ?? user.UserName)
                .Select(user => new MessageRecipientDto
                {
                    Name = string.IsNullOrWhiteSpace(user.Navn) ? user.UserName : user.Navn,
                    Email = user.Email
                })
                .ToList();
        }

        private async Task<IReadOnlyList<ContentItem>> GetSelectedContent(int[] eventIds, int[] newsIds)
        {
            var ids = (eventIds ?? Array.Empty<int>())
                .Concat(newsIds ?? Array.Empty<int>())
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return Array.Empty<ContentItem>();
            }

            return await _db.ContentItems
                .AsNoTracking()
                .Include(item => item.Links)
                .Where(item => ids.Contains(item.Id))
                .OrderBy(item => item.Type)
                .ThenBy(item => item.StartDate ?? item.PublishedAt ?? item.Created)
                .ToListAsync();
        }

        private string GetPublicBaseUrl()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
        }

        private static Dictionary<string, string> BuildTemplateValues(IReadOnlyList<ContentItem> content, EmailBlockDesign blockDesign, string publicBaseUrl)
        {
            var events = content.Where(item => item.Type == ContentTypes.Event).ToList();
            var news = content.Where(item => item.Type == ContentTypes.News).ToList();
            var eventBlocks = RenderContentBlocks(events, blockDesign, publicBaseUrl);
            var newsBlocks = RenderContentBlocks(news, blockDesign, publicBaseUrl);
            var allBlocks = string.Concat(eventBlocks, newsBlocks);

            return new Dictionary<string, string>
            {
                ["ContentBlocks"] = allBlocks,
                ["EventBlocks"] = eventBlocks,
                ["NewsBlocks"] = newsBlocks,
                ["EventTitle"] = events.FirstOrDefault()?.Title ?? string.Empty,
                ["EventStartDate"] = FormatDate(events.FirstOrDefault()?.StartDate),
                ["EventRegisterUrl"] = events.FirstOrDefault() == null ? string.Empty : $"{publicBaseUrl}/react/events/{events.First().Slug}",
                ["ProfileUrl"] = $"{publicBaseUrl}/Identity/Account/Manage"
            };
        }

        private static string RenderContentBlocks(IEnumerable<ContentItem> items, EmailBlockDesign blockDesign, string publicBaseUrl)
        {
            return string.Concat(items.Select(item => RenderContentBlock(item, blockDesign, publicBaseUrl)));
        }

        private static string RenderContentBlock(ContentItem item, EmailBlockDesign blockDesign, string publicBaseUrl)
        {
            var isEvent = item.Type == ContentTypes.Event;
            var accentColor = isEvent ? blockDesign.EventColor : blockDesign.NewsColor;
            var urlType = isEvent ? "events" : "news";
            var url = $"{publicBaseUrl}/react/{urlType}/{item.Slug}";
            var links = item.Links?
                .OrderBy(link => link.SortOrder)
                .Where(link => !string.IsNullOrWhiteSpace(link.Url))
                .ToList() ?? new List<ContentLink>();
            var ctaText = isEvent ? "Tilmeld dig" : "Læs mere";
            var linkItems = string.Concat(links.Select(RenderRelatedLinkButton));
            var eventMeta = isEvent
                ? $@"
  {(string.IsNullOrWhiteSpace(item.Location) ? string.Empty : $@"<p style=""margin:0 0 6px;""><strong>Sted:</strong> {Html(item.Location)}</p>")}
  <p style=""margin:0 0 6px;""><strong>Start:</strong> {Html(FormatDate(item.StartDate))}</p>
  {(item.EndDate.HasValue ? $@"<p style=""margin:0 0 12px;""><strong>Slut:</strong> {Html(FormatDate(item.EndDate))}</p>" : string.Empty)}"
                : string.Empty;
            var body = string.IsNullOrWhiteSpace(item.Body)
                ? string.Empty
                : $@"<div style=""margin:0 0 12px;color:#4f5f73;line-height:1.55;"">{item.Body}</div>";

            return $@"
<div style=""width:90%;margin:0 auto 18px auto;padding:16px;background-color:#f7fbff;border:1px solid #d9e7f5;border-left:6px solid {Html(accentColor)};border-radius:8px;color:#132238;"">
  <p style=""margin:0 0 8px;font-weight:bold;font-size:1.08rem;"">{Html(item.Title)}</p>
  {eventMeta}
  {body}
  {(linkItems.Length > 0 ? $@"<div style=""margin:0 0 12px;"">{linkItems}</div>" : string.Empty)}
  <a href=""{Html(url)}"" target=""_blank"" style=""display:block;width:100%;box-sizing:border-box;text-align:center;background-color:{Html(accentColor)};color:#ffffff;text-decoration:none;border-radius:8px;padding:12px 16px;font-size:0.95rem;font-weight:bold;"">{Html(ctaText)}</a>
</div>";
        }

        private static string RenderRelatedLinkButton(ContentLink link)
        {
            var isPayment = string.Equals(link.Type, "PAYMENT", StringComparison.OrdinalIgnoreCase);
            var label = string.IsNullOrWhiteSpace(link.Label) ? link.Url : link.Label;
            var backgroundColor = isPayment ? "#4353f4" : "#ffffff";
            var textColor = isPayment ? "#ffffff" : "#132238";
            var borderColor = isPayment ? "#4353f4" : "#c9d8ea";

            return $@"<a href=""{Html(link.Url)}"" target=""_blank"" style=""display:block;margin:0 0 10px;padding:12px 16px;background-color:{backgroundColor};color:{textColor};border:1px solid {borderColor};text-align:center;text-decoration:none;border-radius:8px;font-size:0.95rem;font-weight:bold;line-height:1.2;"">{Html(label)}</a>";
        }

        private static string RenderContentBlock(ContentItem item)
        {
            var date = item.Type == ContentTypes.Event ? item.StartDate : item.PublishedAt;
            var urlType = item.Type == ContentTypes.Event ? "events" : "news";
            var url = $"/react/{urlType}/{item.Slug}";
            var primaryLink = item.Links?
                .OrderBy(link => link.SortOrder)
                .FirstOrDefault(link => !string.IsNullOrWhiteSpace(link.Url))?.Url ?? url;

            return $@"
<div style=""width:90%;margin:0 auto 18px auto;padding:16px;background-color:#f7fbff;border:1px solid #d9e7f5;border-left:6px solid #1f78c1;border-radius:8px;"">
  <p style=""margin:0 0 8px;font-weight:bold;font-size:1.05rem;"">{Html(item.Title)}</p>
  <p style=""margin:0 0 10px;color:#4f5f73;"">{Html(item.Summary)}</p>
  <p style=""margin:0 0 10px;""><strong>Hvornår:</strong> {Html(FormatDate(date))}{(string.IsNullOrWhiteSpace(item.Location) ? string.Empty : $"<br><strong>Hvor:</strong> {Html(item.Location)}")}</p>
  <div style=""margin:0 0 12px;color:#4f5f73;"">{item.Body}</div>
  <a href=""{Html(primaryLink)}"" target=""_blank"" style=""display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#1877F2;color:#ffffff;text-decoration:none;border-radius:8px;padding:12px 16px;font-size:0.95rem;font-weight:bold;"">Læs mere</a>
</div>";
        }

        private static string RenderTemplate(string template, Dictionary<string, string> values)
        {
            if (template == null)
            {
                return string.Empty;
            }

            return PlaceholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return values.TryGetValue(key, out var value) ? value ?? string.Empty : match.Value;
            });
        }

        private static MessageMedia NormalizeChannel(string channel)
        {
            if (Enum.TryParse<MessageMedia>(channel, true, out var media))
            {
                return media;
            }

            return MessageMedia.Email;
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToLocalTime().ToString("d. MMMM yyyy 'kl.' HH.mm") : string.Empty;
        }

        private static string Html(string value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private sealed class EmailBlockDesign
        {
            private static readonly Regex MetadataRegex = new(@"<!--\s*GammaEmailBlockDesign\s+eventColor=""([^""]*)""\s+newsColor=""([^""]*)""\s*-->", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            private static readonly Regex ColorRegex = new(@"^#[0-9a-f]{6}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            public string EventColor { get; private init; } = "#1f78c1";

            public string NewsColor { get; private init; } = "#1f78c1";

            public static EmailBlockDesign Parse(string html)
            {
                var match = MetadataRegex.Match(html ?? string.Empty);
                return new EmailBlockDesign
                {
                    EventColor = NormalizeColor(match.Success ? match.Groups[1].Value : null, "#1f78c1"),
                    NewsColor = NormalizeColor(match.Success ? match.Groups[2].Value : null, "#1f78c1")
                };
            }

            public static string RemoveMetadata(string html)
            {
                return MetadataRegex.Replace(html ?? string.Empty, string.Empty);
            }

            private static string NormalizeColor(string value, string fallback)
            {
                return ColorRegex.IsMatch(value ?? string.Empty) ? value : fallback;
            }
        }
    }
}
