using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GamMaSite.Services
{
    /*
     * Encapsulates event/news persistence, filtering, and related-link updates.
     */
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
        private readonly IContentMediaService _contentMedia;
        private readonly ILogger<ContentService> _logger;
        private readonly IEventRegistrationService _eventRegistrations;
        private readonly IStripeService _stripeService;

        public ContentService(
            ApplicationDbContext db,
            IContentMediaService contentMedia = null,
            ILogger<ContentService> logger = null,
            IEventRegistrationService eventRegistrations = null,
            IStripeService stripeService = null)
        {
            _db = db;
            _contentMedia = contentMedia;
            _logger = logger;
            _eventRegistrations = eventRegistrations ?? new EventRegistrationService(db);
            _stripeService = stripeService;
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

            var items = await query.ToListAsync();
            if (string.Equals(type, ContentTypes.Event, StringComparison.OrdinalIgnoreCase))
            {
                var now = DateTime.UtcNow;
                return items
                    .OrderBy(item => item.StartDate.HasValue && item.StartDate.Value > now ? 0 : 1)
                    .ThenBy(item => item.StartDate.HasValue && item.StartDate.Value > now ? item.StartDate : DateTime.MaxValue)
                    .ThenByDescending(item => item.StartDate ?? item.Created)
                    .ToList();
            }

            return items
                .OrderBy(item => item.PublishedAt ?? item.Created)
                .ToList();
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
                MaxParticipants = ValidateMaxParticipants(request.MaxParticipants),
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
            var nextType = NormalizeType(request.Type);
            var nextMaxParticipants = ValidateMaxParticipants(request.MaxParticipants);
            var previousSlug = item.Slug;
            var nextSlug = Required(request.Slug, nameof(request.Slug));
            var oldPictureUrl = item.PictureUrl;
            ContentMediaSlugRename slugRename = null;

            await EnsureMaxParticipantsCanBeReducedAsync(item, nextType, nextMaxParticipants);

            if (_contentMedia != null &&
                !string.Equals(previousSlug, nextSlug, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(oldPictureUrl) &&
                string.Equals(request.PictureUrl, oldPictureUrl, StringComparison.Ordinal) &&
                string.Equals(nextType, item.Type, StringComparison.OrdinalIgnoreCase))
            {
                slugRename = await _contentMedia.PrepareSlugRenameAsync(item, previousSlug, nextSlug);
            }

            item.Title = Required(request.Title, nameof(request.Title));
            item.Slug = nextSlug;
            item.Summary = request.Summary;
            item.Body = request.Body;
            item.PictureUrl = slugRename?.NewUrl ?? request.PictureUrl;
            item.Tags = request.Tags;
            item.Type = nextType;
            item.Status = status;
            item.MaxParticipants = nextMaxParticipants;
            item.ShowOnFrontPage = request.ShowOnFrontPage;
            item.StartDate = request.StartDate;
            item.EndDate = request.EndDate;
            item.Location = request.Location;
            item.Updated = now;
            item.PublishedAt = request.PublishedAt ?? (status == ContentStatuses.Published ? item.PublishedAt ?? now : null);

            _db.ContentLinks.RemoveRange(item.Links);
            item.Links = BuildLinks(request.Links, now);

            await _db.SaveChangesAsync();

            if (string.Equals(nextType, ContentTypes.Event, StringComparison.OrdinalIgnoreCase))
            {
                await _eventRegistrations.PromoteWaitlistedAsync(item.Id);
            }

            if (slugRename != null)
            {
                try
                {
                    await _contentMedia.MoveLocalFileAsync(slugRename);
                }
                catch (ContentMediaStorageException)
                {
                    item.Slug = previousSlug;
                    item.PictureUrl = oldPictureUrl;
                    item.Updated = DateTime.UtcNow;
                    try
                    {
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception rollbackException)
                    {
                        _logger?.LogError(rollbackException,
                            "Could not restore picture URL for content item {ContentItemId} after its image rename failed.", item.Id);
                    }

                    throw;
                }
            }
            else if (_contentMedia != null && !string.Equals(oldPictureUrl, item.PictureUrl, StringComparison.Ordinal))
            {
                await DeleteLocalFileBestEffortAsync(oldPictureUrl);
            }

            return await GetByIdAsync(item.Id, true);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _db.ContentItems.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            var pictureUrl = item.PictureUrl;
            var links = await _db.ContentLinks.Where(link => link.ContentItemId == id).ToListAsync();
            var registrations = await _db.EventRegistrations.Where(registration => registration.ContentItemId == id).ToListAsync();
            var offers = await _db.PaymentOffers.Where(offer => offer.ContentItemId == id).ToListAsync();
            var stripeTargets = await GetStripeCleanupTargetsAsync(offers);
            var deactivatedPrices = new List<string>();
            var deactivatedProducts = new List<string>();
            IDbContextTransaction transaction = null;

            try
            {
                transaction = await BeginContentTransactionAsync();

                _db.ContentLinks.RemoveRange(links);
                _db.EventRegistrations.RemoveRange(registrations);
                _db.PaymentOffers.RemoveRange(offers);
                _db.ContentItems.Remove(item);
                await _db.SaveChangesAsync();

                if (stripeTargets.Prices.Count > 0 || stripeTargets.Products.Count > 0)
                {
                    if (_stripeService == null)
                    {
                        throw new ContentDeletionException("Stripe-tjenesten er ikke tilgængelig, så eventets betalingsprodukter ikke kunne arkiveres.");
                    }

                    foreach (var priceId in stripeTargets.Prices)
                    {
                        if (!await TryDeactivateStripePriceAsync(priceId))
                        {
                            throw new ContentDeletionException("En Stripe-pris kunne ikke arkiveres.");
                        }

                        deactivatedPrices.Add(priceId);
                    }

                    foreach (var productId in stripeTargets.Products)
                    {
                        if (!await TryDeactivateStripeProductAsync(productId))
                        {
                            throw new ContentDeletionException("Et Stripe-produkt kunne ikke arkiveres.");
                        }

                        deactivatedProducts.Add(productId);
                    }
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                if (_contentMedia != null)
                {
                    await DeleteLocalFileBestEffortAsync(pictureUrl);
                }

                return true;
            }
            catch (ContentDeletionException)
            {
                await RestoreStripeDefinitionsAsync(deactivatedPrices, deactivatedProducts);

                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                else
                {
                    await RestoreDeletedContentAsync(item, links, registrations, offers);
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private async Task<StripeCleanupTargets> GetStripeCleanupTargetsAsync(IReadOnlyCollection<PaymentOffer> offers)
        {
            var stripeOffers = offers
                .Where(offer => string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var offerIds = offers.Select(offer => offer.Id).ToList();
            var productIds = stripeOffers
                .Select(offer => offer.ProviderProductId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var priceIds = stripeOffers
                .Select(offer => offer.ProviderPriceId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var usedProductIds = productIds.Count == 0
                ? new List<string>()
                : await _db.PaymentOffers
                    .Where(offer => !offerIds.Contains(offer.Id)
                        && offer.Provider == PaymentOfferProviders.Stripe
                        && productIds.Contains(offer.ProviderProductId))
                    .Select(offer => offer.ProviderProductId)
                    .Distinct()
                    .ToListAsync();
            var usedPriceIds = priceIds.Count == 0
                ? new List<string>()
                : await _db.PaymentOffers
                    .Where(offer => !offerIds.Contains(offer.Id)
                        && offer.Provider == PaymentOfferProviders.Stripe
                        && priceIds.Contains(offer.ProviderPriceId))
                    .Select(offer => offer.ProviderPriceId)
                    .Distinct()
                    .ToListAsync();

            return new StripeCleanupTargets(
                priceIds.Where(id => !usedPriceIds.Contains(id, StringComparer.Ordinal)).ToList(),
                productIds.Where(id => !usedProductIds.Contains(id, StringComparer.Ordinal)).ToList());
        }

        private async Task<bool> TryDeactivateStripePriceAsync(string priceId)
        {
            try
            {
                return await _stripeService.DeactivatePriceAsync(priceId);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(exception, "Could not archive Stripe price {PriceId} while deleting content.", priceId);
                return false;
            }
        }

        private async Task<bool> TryDeactivateStripeProductAsync(string productId)
        {
            try
            {
                return await _stripeService.DeactivateProductAsync(productId);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(exception, "Could not archive Stripe product {ProductId} while deleting content.", productId);
                return false;
            }
        }

        private async Task RestoreStripeDefinitionsAsync(
            IReadOnlyCollection<string> priceIds,
            IReadOnlyCollection<string> productIds)
        {
            if (_stripeService == null)
            {
                return;
            }

            foreach (var priceId in priceIds)
            {
                try
                {
                    await _stripeService.SetPriceActiveAsync(priceId, true);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Could not restore Stripe price {PriceId} after content deletion failed.", priceId);
                }
            }

            foreach (var productId in productIds)
            {
                try
                {
                    await _stripeService.SetProductActiveAsync(productId, true);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Could not restore Stripe product {ProductId} after content deletion failed.", productId);
                }
            }
        }

        private async Task RestoreDeletedContentAsync(
            ContentItem item,
            IReadOnlyCollection<ContentLink> links,
            IReadOnlyCollection<EventRegistration> registrations,
            IReadOnlyCollection<PaymentOffer> offers)
        {
            _db.ChangeTracker.Clear();
            _db.ContentItems.Add(item);
            _db.ContentLinks.AddRange(links);
            _db.EventRegistrations.AddRange(registrations);
            _db.PaymentOffers.AddRange(offers);
            await _db.SaveChangesAsync();
        }

        private Task<IDbContextTransaction> BeginContentTransactionAsync()
        {
            return _db.Database.IsRelational()
                ? _db.Database.BeginTransactionAsync()
                : Task.FromResult<IDbContextTransaction>(null);
        }

        private sealed record StripeCleanupTargets(IReadOnlyList<string> Prices, IReadOnlyList<string> Products);

        private async Task DeleteLocalFileBestEffortAsync(string pictureUrl)
        {
            const int attempts = 2;
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    await _contentMedia.DeleteLocalFileIfUnreferencedAsync(pictureUrl);
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex,
                        "Could not clean up unreferenced content image {PictureUrl} (attempt {Attempt}/{Attempts}).",
                        pictureUrl, attempt, attempts);

                    if (attempt < attempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                }
            }
        }

        private static IQueryable<ContentItem> IncludeLinks(IQueryable<ContentItem> query)
        {
            return query
                .Include(item => item.Links.OrderBy(link => link.SortOrder))
                .Include(item => item.PaymentOffers
                    .Where(offer => offer.IsActive)
                    .OrderBy(offer => offer.Created)
                    .ThenBy(offer => offer.Id));
        }

        private static int? ValidateMaxParticipants(int? maxParticipants)
        {
            if (maxParticipants < 0)
            {
                throw new ArgumentException("MaxParticipants must be zero or greater");
            }

            return maxParticipants;
        }

        private async Task EnsureMaxParticipantsCanBeReducedAsync(ContentItem item, string nextType, int? nextMaxParticipants)
        {
            if (!string.Equals(nextType, ContentTypes.Event, StringComparison.OrdinalIgnoreCase)
                || !nextMaxParticipants.HasValue
                || item.MaxParticipants == nextMaxParticipants)
            {
                return;
            }

            var participantCount = await _db.EventRegistrations
                .AsNoTracking()
                .CountAsync(registration => registration.ContentItemId == item.Id
                    && (registration.RegistrationType == RegistrationTypes.Attendee
                        || registration.RegistrationType == RegistrationTypes.Organizer));

            if (participantCount > nextMaxParticipants.Value)
            {
                throw new ArgumentException("Der angivne maks. antal deltagere er lavere end det nuværende antal deltagere og arrangører.");
            }
        }

        private static List<ContentLink> BuildLinks(IEnumerable<SaveContentLinkRequest> links, DateTime now)
        {
            return (links ?? Enumerable.Empty<SaveContentLinkRequest>())
                .Select(link => new ContentLink
                {
                    Label = Required(link.Label, nameof(link.Label)),
                    Url = SecureUrl(link.Url),
                    Type = Required(link.Type, nameof(link.Type)).ToUpperInvariant(),
                    SortOrder = link.SortOrder,
                    Created = now,
                    Updated = now
                })
                .ToList();
        }

        private static string SecureUrl(string value)
        {
            var url = Required(value, nameof(value));
            if (url.StartsWith("/react/", StringComparison.Ordinal))
            {
                return url;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Content links must use an absolute HTTPS URL");
            }

            return url;
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
