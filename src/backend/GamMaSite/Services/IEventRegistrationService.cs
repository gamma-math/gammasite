using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;

namespace GamMaSite.Services
{
    /*
     * Defines event registration operations for user signups and admin attendee lists.
     */
    public interface IEventRegistrationService
    {
        Task<EventRegistration> RegisterAsync(int contentItemId, string userId, SaveEventRegistrationRequest request);

        Task<EventRegistration> AddAsync(int contentItemId, AddEventRegistrationRequest request);

        Task<EventRegistration> GetRegistrationAsync(int contentItemId, string userId);

        Task<EventRegistration> UpdateAsync(int contentItemId, int registrationId, UpdateEventRegistrationRequest request);

        Task<bool> UnregisterAsync(int contentItemId, string userId);

        Task<IReadOnlyList<EventRegistration>> GetRegistrationsAsync(int contentItemId);
    }
}
