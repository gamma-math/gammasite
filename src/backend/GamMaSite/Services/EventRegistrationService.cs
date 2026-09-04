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
            RegistrationTypes.Declined
        };

        private readonly ApplicationDbContext _db;

        public EventRegistrationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<EventRegistration> RegisterAsync(int contentItemId, string userId, SaveEventRegistrationRequest request)
        {
            var content = await _db.ContentItems.FindAsync(contentItemId);
            if (content == null || content.Type != ContentTypes.Event)
            {
                throw new ArgumentException("Content item is not an event");
            }

            var now = DateTime.UtcNow;
            var registrationType = NormalizeRegistrationType(request?.RegistrationType);
            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);

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
            registration.Registered = registrationType != RegistrationTypes.Declined;
            registration.ResponseText = request?.ResponseText;
            registration.Updated = now;

            await _db.SaveChangesAsync();

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

            registration.RegistrationType = NormalizeRegistrationType(request.RegistrationType);
            registration.Registered = request.Registered;
            registration.ResponseText = request.ResponseText;
            registration.Updated = now;

            await _db.SaveChangesAsync();

            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstAsync(item => item.Id == registration.Id);
        }

        public async Task<bool> UnregisterAsync(int contentItemId, string userId)
        {
            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);

            if (registration == null)
            {
                return false;
            }

            _db.EventRegistrations.Remove(registration);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<EventRegistration> GetRegistrationAsync(int contentItemId, string userId)
        {
            return await _db.EventRegistrations
                .Include(item => item.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);
        }

        public async Task<EventRegistration> UpdateAsync(int contentItemId, int registrationId, UpdateEventRegistrationRequest request)
        {
            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.Id == registrationId && item.ContentItemId == contentItemId);

            if (registration == null)
            {
                return null;
            }

            registration.RegistrationType = NormalizeRegistrationType(request?.RegistrationType);
            registration.Registered = request?.Registered ?? registration.Registered;
            registration.ResponseText = request?.ResponseText;
            registration.Updated = DateTime.UtcNow;

            await _db.SaveChangesAsync();

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
    }
}
