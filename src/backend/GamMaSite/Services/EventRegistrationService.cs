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

        private readonly ApplicationDbContext _db;

        public EventRegistrationService(ApplicationDbContext db)
        {
            _db = db;
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
            registration.ResponseText = request?.ResponseText;
            registration.Updated = now;

            await _db.SaveChangesAsync();
            if (currentCountsTowardsCapacity && !CountsTowardsCapacity(registrationType))
            {
                await PromoteWaitlistedAsync(content, registration.Id);
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);

            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstAsync(item => item.Id == registration.Id);
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
            registration.ResponseText = request.ResponseText;
            registration.Updated = now;

            await _db.SaveChangesAsync();
            if (currentCountsTowardsCapacity && !nextCountsTowardsCapacity)
            {
                await PromoteWaitlistedAsync(content, registration.Id);
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);

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
            await PromoteWaitlistedAsync(content, null);
            await _db.SaveChangesAsync();
            await CommitAsync(transaction);
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
            await PromoteWaitlistedAsync(content, null);
            await _db.SaveChangesAsync();
            await CommitAsync(transaction);
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

            await PromoteWaitlistedAsync(content, null);
            await _db.SaveChangesAsync();
            await CommitAsync(transaction);
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
                var content = await _db.ContentItems.FindAsync(contentItemId);
                await EnsureCapacityAvailableAsync(content, registration.Id);
            }

            registration.RegistrationType = registrationType;
            registration.Registered = registered;
            registration.ResponseText = request?.ResponseText;
            registration.Updated = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            if (currentCountsTowardsCapacity && !nextCountsTowardsCapacity)
            {
                var content = await _db.ContentItems.FindAsync(contentItemId);
                await PromoteWaitlistedAsync(content, registration.Id);
                await _db.SaveChangesAsync();
            }
            await CommitAsync(transaction);

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

        private async Task PromoteWaitlistedAsync(ContentItem content, int? excludedRegistrationId)
        {
            if (content == null)
            {
                return;
            }

            var confirmedParticipants = await _db.EventRegistrations
                .CountAsync(item => item.ContentItemId == content.Id &&
                    (item.RegistrationType == RegistrationTypes.Attendee || item.RegistrationType == RegistrationTypes.Organizer));

            var availableSlots = content.MaxParticipants.HasValue
                ? content.MaxParticipants.Value - confirmedParticipants
                : int.MaxValue;
            if (availableSlots <= 0)
            {
                return;
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
        }
    }
}
