using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Services
{
    public class ContentService : IContentService
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ContentTypes.News,
            ContentTypes.Event
        };

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            ContentStatuses.Draft,
            ContentStatuses.Published,
            ContentStatuses.Archived
        };

        private readonly ApplicationDbContext _db;

        public ContentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ContentItem>> GetPublishedAsync(string type, bool frontPageOnly = false)
        {
            var query = IncludeLinks(_db.ContentItems.AsNoTracking())
                .Where(item => item.Status == ContentStatuses.Published)
                .Where(item => string.IsNullOrWhiteSpace(type) || item.Type == NormalizeType(type));

            if (frontPageOnly)
            {
                query = query.Where(item => item.ShowOnFrontPage);
            }

            return await query
                .OrderByDescending(item => item.PublishedAt ?? item.Created)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ContentItem>> GetAllAsync(string type, string status)
        {
            var query = IncludeLinks(_db.ContentItems.AsNoTracking());

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(item => item.Type == NormalizeType(type));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(item => item.Status == NormalizeStatus(status));
            }

            return await query
                .OrderByDescending(item => item.Updated)
                .ToListAsync();
        }

        public async Task<ContentItem> GetByIdAsync(int id, bool includeUnpublished)
        {
            var query = IncludeLinks(_db.ContentItems.AsNoTracking());
            if (!includeUnpublished)
            {
                query = query.Where(item => item.Status == ContentStatuses.Published);
            }

            return await query.FirstOrDefaultAsync(item => item.Id == id);
        }

        public async Task<ContentItem> GetBySlugAsync(string slug, bool includeUnpublished)
        {
            var query = IncludeLinks(_db.ContentItems.AsNoTracking());
            if (!includeUnpublished)
            {
                query = query.Where(item => item.Status == ContentStatuses.Published);
            }

            return await query.FirstOrDefaultAsync(item => item.Slug == slug);
        }

        public async Task<ContentItem> CreateAsync(SaveContentItemRequest request, string createdByUserId)
        {
            var now = DateTime.UtcNow;
            var status = NormalizeStatus(request.Status);
            var item = new ContentItem
            {
                Title = Required(request.Title, nameof(request.Title)),
                Slug = Required(request.Slug, nameof(request.Slug)),
                Summary = request.Summary,
                Body = request.Body,
                PictureUrl = request.PictureUrl,
                Tags = request.Tags,
                Type = NormalizeType(request.Type),
                Status = status,
                ShowOnFrontPage = request.ShowOnFrontPage,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Location = request.Location,
                CreatedByUserId = createdByUserId,
                Created = now,
                Updated = now,
                PublishedAt = request.PublishedAt ?? (status == ContentStatuses.Published ? now : null),
                Links = BuildLinks(request.Links, now)
            };

            _db.ContentItems.Add(item);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(item.Id, true);
        }

        public async Task<ContentItem> UpdateAsync(int id, SaveContentItemRequest request)
        {
            var item = await _db.ContentItems
                .Include(content => content.Links)
                .FirstOrDefaultAsync(content => content.Id == id);

            if (item == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var status = NormalizeStatus(request.Status);

            item.Title = Required(request.Title, nameof(request.Title));
            item.Slug = Required(request.Slug, nameof(request.Slug));
            item.Summary = request.Summary;
            item.Body = request.Body;
            item.PictureUrl = request.PictureUrl;
            item.Tags = request.Tags;
            item.Type = NormalizeType(request.Type);
            item.Status = status;
            item.ShowOnFrontPage = request.ShowOnFrontPage;
            item.StartDate = request.StartDate;
            item.EndDate = request.EndDate;
            item.Location = request.Location;
            item.Updated = now;
            item.PublishedAt = request.PublishedAt ?? (status == ContentStatuses.Published ? item.PublishedAt ?? now : null);

            _db.ContentLinks.RemoveRange(item.Links);
            item.Links = BuildLinks(request.Links, now);

            await _db.SaveChangesAsync();

            return await GetByIdAsync(item.Id, true);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _db.ContentItems.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            _db.ContentItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        private static IQueryable<ContentItem> IncludeLinks(IQueryable<ContentItem> query)
        {
            return query.Include(item => item.Links.OrderBy(link => link.SortOrder));
        }

        private static List<ContentLink> BuildLinks(IEnumerable<SaveContentLinkRequest> links, DateTime now)
        {
            return (links ?? Enumerable.Empty<SaveContentLinkRequest>())
                .Select(link => new ContentLink
                {
                    Label = Required(link.Label, nameof(link.Label)),
                    Url = Required(link.Url, nameof(link.Url)),
                    Type = Required(link.Type, nameof(link.Type)).ToUpperInvariant(),
                    SortOrder = link.SortOrder,
                    Created = now,
                    Updated = now
                })
                .ToList();
        }

        private static string NormalizeType(string type)
        {
            var normalized = Required(type, nameof(type)).ToUpperInvariant();
            if (!AllowedTypes.Contains(normalized))
            {
                throw new ArgumentException($"Unsupported content type: {type}");
            }

            return normalized;
        }

        private static string NormalizeStatus(string status)
        {
            var normalized = string.IsNullOrWhiteSpace(status)
                ? ContentStatuses.Draft
                : status.Trim().ToUpperInvariant();

            if (!AllowedStatuses.Contains(normalized))
            {
                throw new ArgumentException($"Unsupported content status: {status}");
            }

            return normalized;
        }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} is required");
            }

            return value.Trim();
        }
    }
}
