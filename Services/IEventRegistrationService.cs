using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;

namespace GamMaSite.Services
{
    public interface IEventRegistrationService
    {
        Task<EventRegistration> RegisterAsync(int contentItemId, string userId, SaveEventRegistrationRequest request);

        Task<bool> UnregisterAsync(int contentItemId, string userId);

        Task<IReadOnlyList<EventRegistration>> GetRegistrationsAsync(int contentItemId);
    }
}
