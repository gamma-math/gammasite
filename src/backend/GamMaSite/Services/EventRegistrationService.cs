using System;
using System.Collections.Generic;
using System.Data;
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
     * Encapsulates event registration lookup, signup, and admin attendee updates.
     */
    public class EventRegistrationService : IEventRegistrationService
    {
        private static readonly HashSet<string> AllowedRegistrationTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            RegistrationTypes.Attendee,
            RegistrationTypes.Organizer,
            RegistrationTypes.Interested,
            RegistrationTypes.Declined,
            RegistrationTypes.Waitlisted
        };

        private const string CapacityReachedMessage = "Der er allerede maks antal deltagere.";
        private const int MaxResponseTextLength = 1024;

        private readonly ApplicationDbContext _db;
        private readonly ISystemEmailTemplateService _systemEmailTemplateService;
        private readonly ILogger<EventRegistrationService> _logger;

        public EventRegistrationService(
            ApplicationDbContext db,
            ISystemEmailTemplateService systemEmailTemplateService = null,
            ILogger<EventRegistrationService> logger = null)
        {
            _db = db;
            _systemEmailTemplateService = systemEmailTemplateService;
            _logger = logger;
        }

        public async Task<EventRegistration> RegisterAsync(int contentItemId, string userId, SaveEventRegistrationRequest request)
        {
            await using var transaction = await BeginEventTransactionAsync(contentItemId);
            var content = await _db.ContentItems.FindAsync(contentItemId);
            if (content == null || content.Type != ContentTypes.Event)
            {
                throw new ArgumentException("Content item is not an event");
            }

            EnsureRegistrationOpen(content);

            var now = DateTime.UtcNow;
            var registrationType = NormalizeRegistrationType(request?.RegistrationType);
            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);

            var currentCountsTowardsCapacity = registration != null && CountsTowardsCapacity(registration.RegistrationType);
            if (registrationType == RegistrationTypes.Attendee && !currentCountsTowardsCapacity && !await HasCapacityAsync(content, registration?.Id))
            {
                registrationType = RegistrationTypes.Waitlisted;
            }
            else if (CountsTowardsCapacity(registrationType) && !currentCountsTowardsCapacity)
            {
                await EnsureCapacityAvailableAsync(content, registration?.Id);
            }

            if (registration == null)
            {
                registration = new EventRegistration
                {
                    ContentItemId = contentItemId,
                    UserId = userId,
                    Created = now
                };
                _db.EventRegistrations.Add(registration);
            }

            registration.RegistrationType = registrationType;
            registration.Registered = false;
            registration.ResponseText = NormalizeResponseText(request?.ResponseText);
            registration.Updated = now;

            await _db.SaveChangesAsync();
            var promotedRegistrationIds = currentCountsTowardsCapacity && !CountsTowardsCapacity(registrationType)
                ? await PromoteWaitlistedAsync(content, registration.Id)
                : Array.Empty<int>();
            if (promotedRegistrationIds.Count > 0)
            {
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);

            var savedRegistration = await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstAsync(item => item.Id == registration.Id);

            await SendEventConfirmationAsync(savedRegistration, content);
            await SendWaitlistPromotionEmailsAsync(content, promotedRegistrationIds);

            return savedRegistration;
        }

        public async Task<EventRegistration> AddAsync(int contentItemId, AddEventRegistrationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.UserId))
            {
                throw new ArgumentException("UserId is required");
            }

            await using var transaction = await BeginEventTransactionAsync(contentItemId);
            var content = await _db.ContentItems.FindAsync(contentItemId);
            if (content == null || content.Type != ContentTypes.Event)
            {
                throw new ArgumentException("Content item is not an event");
            }

            var userExists = await _db.Users.AnyAsync(user => user.Id == request.UserId);
            if (!userExists)
            {
                throw new ArgumentException("User does not exist");
            }

            var now = DateTime.UtcNow;
            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == request.UserId);
            var existingRegistrationId = registration?.Id;
            var currentCountsTowardsCapacity = registration != null && CountsTowardsCapacity(registration.RegistrationType);

            if (registration == null)
            {
                registration = new EventRegistration
                {
                    ContentItemId = contentItemId,
                    UserId = request.UserId,
                    Created = now
                };
                _db.EventRegistrations.Add(registration);
            }

            var registrationType = NormalizeRegistrationType(request.RegistrationType);
            var nextCountsTowardsCapacity = CountsTowardsCapacity(registrationType);
            if (nextCountsTowardsCapacity && !currentCountsTowardsCapacity)
            {
                await EnsureCapacityAvailableAsync(content, existingRegistrationId);
            }

            registration.RegistrationType = registrationType;
            registration.Registered = request.Registered;
            registration.ResponseText = NormalizeResponseText(request.ResponseText);
            registration.Updated = now;

            await _db.SaveChangesAsync();
            var promotedRegistrationIds = currentCountsTowardsCapacity && !nextCountsTowardsCapacity
                ? await PromoteWaitlistedAsync(content, registration.Id)
                : Array.Empty<int>();
            if (promotedRegistrationIds.Count > 0)
            {
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);

            await SendWaitlistPromotionEmailsAsync(content, promotedRegistrationIds);

            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstAsync(item => item.Id == registration.Id);
        }

        public async Task<bool> UnregisterAsync(int contentItemId, string userId)
        {
            await using var transaction = await BeginEventTransactionAsync(contentItemId);
            var content = await _db.ContentItems.FindAsync(contentItemId);
            if (content == null || content.Type != ContentTypes.Event)
            {
                return false;
            }

            EnsureRegistrationCancellationOpen(content);

            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);

            if (registration == null)
            {
                return false;
            }

            _db.EventRegistrations.Remove(registration);
            await _db.SaveChangesAsync();
            var promotedRegistrationIds = await PromoteWaitlistedAsync(content, null);
            if (promotedRegistrationIds.Count > 0)
            {
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);
            await SendWaitlistPromotionEmailsAsync(content, promotedRegistrationIds);
            return true;
        }

        public async Task<bool> DeleteAsync(int contentItemId, int registrationId)
        {
            await using var transaction = await BeginEventTransactionAsync(contentItemId);
            var content = await _db.ContentItems.FindAsync(contentItemId);
            if (content == null || content.Type != ContentTypes.Event)
            {
                return false;
            }

            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.Id == registrationId);

            if (registration == null)
            {
                return false;
            }

            _db.EventRegistrations.Remove(registration);
            await _db.SaveChangesAsync();
            var promotedRegistrationIds = await PromoteWaitlistedAsync(content, null);
            if (promotedRegistrationIds.Count > 0)
            {
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);
            await SendWaitlistPromotionEmailsAsync(content, promotedRegistrationIds);
            return true;
        }

        private static void EnsureRegistrationOpen(ContentItem content)
        {
            if (!EventRegistrationRules.IsRegistrationOpen(content, DateTime.UtcNow))
            {
                throw new ArgumentException("Registration for this event is closed");
            }
        }

        private static void EnsureRegistrationCancellationOpen(ContentItem content)
        {
            if (!EventRegistrationRules.IsRegistrationCancellationOpen(content, DateTime.UtcNow))
            {
                throw new ArgumentException("Registration changes for this event are closed");
            }
        }

        public async Task<EventRegistration> GetRegistrationAsync(int contentItemId, string userId)
        {
            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);
        }

        public async Task<EventRegistration> UpdateMyNoteAsync(int contentItemId, string userId, string responseText)
        {
            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);

            if (registration == null)
            {
                return null;
            }

            registration.ResponseText = NormalizeResponseText(responseText);
            registration.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstAsync(item => item.Id == registration.Id);
        }

        public async Task<IReadOnlyList<AccountEventRegistrationDto>> GetAccountRegistrationsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Array.Empty<AccountEventRegistrationDto>();
            }

            return await _db.EventRegistrations
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.ContentItem.Type == ContentTypes.Event)
                .OrderByDescending(item => item.ContentItem.StartDate ?? DateTime.MinValue)
                .ThenByDescending(item => item.Id)
                .Select(item => new AccountEventRegistrationDto
                {
                    EventId = item.ContentItemId,
                    Title = item.ContentItem.Title,
                    Slug = item.ContentItem.Slug,
                    StartDate = item.ContentItem.StartDate,
                    EndDate = item.ContentItem.EndDate,
                    RegistrationType = item.RegistrationType,
                    Registered = item.Registered
                })
                .ToListAsync();
        }

        public async Task<EventRegistrationSummaryDto> GetSummaryAsync(int contentItemId, bool includeUnpublished = false)
        {
            var content = await _db.ContentItems
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == contentItemId
                    && item.Type == ContentTypes.Event
                    && (includeUnpublished || item.Status == ContentStatuses.Published));

            if (content == null)
            {
                return null;
            }

            var confirmedParticipants = await _db.EventRegistrations
                .AsNoTracking()
                .CountAsync(item => item.ContentItemId == contentItemId &&
                    (item.RegistrationType == RegistrationTypes.Attendee || item.RegistrationType == RegistrationTypes.Organizer));
            var waitlistedParticipants = await _db.EventRegistrations
                .AsNoTracking()
                .CountAsync(item => item.ContentItemId == contentItemId && item.RegistrationType == RegistrationTypes.Waitlisted);

            return new EventRegistrationSummaryDto
            {
                ContentItemId = contentItemId,
                MaxParticipants = content.MaxParticipants,
                ConfirmedParticipants = confirmedParticipants,
                WaitlistedParticipants = waitlistedParticipants,
                IsFull = content.MaxParticipants.HasValue && confirmedParticipants >= content.MaxParticipants.Value
            };
        }

        public async Task PromoteWaitlistedAsync(int contentItemId)
        {
            await using var transaction = await BeginEventTransactionAsync(contentItemId);
            var content = await _db.ContentItems
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == contentItemId);
            if (content == null || content.Type != ContentTypes.Event)
            {
                return;
            }

            var promotedRegistrationIds = await PromoteWaitlistedAsync(content, null);
            if (promotedRegistrationIds.Count > 0)
            {
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);
            await SendWaitlistPromotionEmailsAsync(content, promotedRegistrationIds);
        }

        public async Task<EventRegistration> UpdateAsync(int contentItemId, int registrationId, UpdateEventRegistrationRequest request)
        {
            await using var transaction = await BeginEventTransactionAsync(contentItemId);
            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.Id == registrationId && item.ContentItemId == contentItemId);

            if (registration == null)
            {
                return null;
            }

            var registrationType = NormalizeRegistrationType(request?.RegistrationType);
            var registered = request?.Registered ?? registration.Registered;
            var currentCountsTowardsCapacity = CountsTowardsCapacity(registration.RegistrationType);
            var nextCountsTowardsCapacity = CountsTowardsCapacity(registrationType);
            if (nextCountsTowardsCapacity && !currentCountsTowardsCapacity)
            {
                var currentContent = await _db.ContentItems.FindAsync(contentItemId);
                await EnsureCapacityAvailableAsync(currentContent, registration.Id);
            }

            registration.RegistrationType = registrationType;
            registration.Registered = registered;
            registration.ResponseText = NormalizeResponseText(request?.ResponseText);
            registration.Updated = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            var content = await _db.ContentItems.FindAsync(contentItemId);
            var promotedRegistrationIds = currentCountsTowardsCapacity && !nextCountsTowardsCapacity
                ? await PromoteWaitlistedAsync(content, registration.Id)
                : Array.Empty<int>();
            if (promotedRegistrationIds.Count > 0)
            {
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);
            await SendWaitlistPromotionEmailsAsync(content, promotedRegistrationIds);

            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstAsync(item => item.Id == registration.Id);
        }

        public async Task<IReadOnlyList<EventRegistration>> GetRegistrationsAsync(int contentItemId)
        {
            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .Where(item => item.ContentItemId == contentItemId)
                .OrderBy(item => item.Created)
                .ToListAsync();
        }

        private static string NormalizeRegistrationType(string registrationType)
        {
            var normalized = string.IsNullOrWhiteSpace(registrationType)
                ? RegistrationTypes.Attendee
                : registrationType.Trim().ToUpperInvariant();

            if (!AllowedRegistrationTypes.Contains(normalized))
            {
                throw new ArgumentException($"Unsupported registration type: {registrationType}");
            }

            return normalized;
        }

        private static string NormalizeResponseText(string responseText)
        {
            var normalized = string.IsNullOrWhiteSpace(responseText) ? null : responseText.Trim();
            if (normalized?.Length > MaxResponseTextLength)
            {
                throw new ArgumentException($"Noten må højst være {MaxResponseTextLength} tegn.");
            }

            return normalized;
        }

        private async Task<IDbContextTransaction> BeginEventTransactionAsync(int contentItemId)
        {
            if (!_db.Database.IsRelational())
            {
                return null;
            }

            var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT `Id` FROM `ContentItems` WHERE `Id` = {contentItemId} FOR UPDATE");
            return transaction;
        }

        private static async Task CommitAsync(IDbContextTransaction transaction)
        {
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }

        private static bool CountsTowardsCapacity(string registrationType)
        {
            return registrationType == RegistrationTypes.Attendee || registrationType == RegistrationTypes.Organizer;
        }

        private async Task<bool> HasCapacityAsync(ContentItem content, int? excludedRegistrationId)
        {
            if (!content.MaxParticipants.HasValue)
            {
                return true;
            }

            var query = _db.EventRegistrations
                .Where(item => item.ContentItemId == content.Id &&
                    (item.RegistrationType == RegistrationTypes.Attendee || item.RegistrationType == RegistrationTypes.Organizer));
            if (excludedRegistrationId.HasValue)
            {
                query = query.Where(item => item.Id != excludedRegistrationId.Value);
            }

            var confirmedParticipants = await query.CountAsync();
            return confirmedParticipants < content.MaxParticipants.Value;
        }

        private async Task EnsureCapacityAvailableAsync(ContentItem content, int? excludedRegistrationId)
        {
            if (content == null || await HasCapacityAsync(content, excludedRegistrationId))
            {
                return;
            }

            throw new ArgumentException(CapacityReachedMessage);
        }

        private async Task<IReadOnlyList<int>> PromoteWaitlistedAsync(ContentItem content, int? excludedRegistrationId)
        {
            if (content == null)
            {
                return Array.Empty<int>();
            }

            var confirmedParticipants = await _db.EventRegistrations
                .CountAsync(item => item.ContentItemId == content.Id &&
                    (item.RegistrationType == RegistrationTypes.Attendee || item.RegistrationType == RegistrationTypes.Organizer));

            var availableSlots = content.MaxParticipants.HasValue
                ? content.MaxParticipants.Value - confirmedParticipants
                : int.MaxValue;
            if (availableSlots <= 0)
            {
                return Array.Empty<int>();
            }

            var waitlisted = _db.EventRegistrations
                .Where(item => item.ContentItemId == content.Id && item.RegistrationType == RegistrationTypes.Waitlisted);
            if (excludedRegistrationId.HasValue)
            {
                waitlisted = waitlisted.Where(item => item.Id != excludedRegistrationId.Value);
            }

            var nextRegistrations = await waitlisted
                .OrderBy(item => item.Created)
                .ThenBy(item => item.Id)
                .Take(availableSlots)
                .ToListAsync();

            foreach (var next in nextRegistrations)
            {
                next.RegistrationType = RegistrationTypes.Attendee;
                next.Updated = DateTime.UtcNow;
            }

            return nextRegistrations.Select(item => item.Id).ToList();
        }

        private async Task SendEventConfirmationAsync(EventRegistration registration, ContentItem content)
        {
            if (!EventNotificationSettingsPolicy.ShouldSendRegistrationConfirmation(content?.EventNotificationSettings) ||
                _systemEmailTemplateService == null || registration?.User == null || string.IsNullOrWhiteSpace(registration.User.Email))
            {
                return;
            }

            try
            {
                await _systemEmailTemplateService.SendEventConfirmationAsync(
                    registration.User.Email,
                    registration.User.Navn,
                    content,
                    registration.RegistrationType);
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(
                    exception,
                    "Could not send event registration confirmation email for content item {ContentItemId}.",
                    content.Id);
            }
        }

        private async Task SendWaitlistPromotionEmailsAsync(ContentItem content, IReadOnlyList<int> registrationIds)
        {
            if (!EventNotificationSettingsPolicy.ShouldSendWaitlistPromotion(content?.EventNotificationSettings) ||
                _systemEmailTemplateService == null || registrationIds == null || registrationIds.Count == 0)
            {
                return;
            }

            try
            {
                var registrations = await _db.EventRegistrations
                    .Include(item => item.User)
                    .AsNoTracking()
                    .Where(item => registrationIds.Contains(item.Id))
                    .ToListAsync();

                foreach (var registration in registrations)
                {
                    if (registration.User == null || string.IsNullOrWhiteSpace(registration.User.Email))
                    {
                        continue;
                    }

                    try
                    {
                        await _systemEmailTemplateService.SendWaitlistPromotionAsync(
                            registration.User.Email,
                            registration.User.Navn,
                            content);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogWarning(
                            exception,
                            "Could not send waitlist promotion email for content item {ContentItemId} and registration {RegistrationId}.",
                            content.Id,
                            registration.Id);
                    }
                }
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(
                    exception,
                    "Could not load registrations for waitlist promotion emails for content item {ContentItemId}.",
                    content.Id);
            }
        }
    }
}
