using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;

namespace GamMaSite.Services
{
    /*
     * Defines content operations used by API controllers and React admin screens.
     */
    public interface IContentService
    {
        Task<IReadOnlyList<ContentItem>> GetPublishedAsync(string type, bool frontPageOnly = false);

        Task<IReadOnlyList<ContentItem>> GetAllAsync(string type, string status);

        Task<ContentItem> GetByIdAsync(int id, bool includeUnpublished);

        Task<ContentItem> GetBySlugAsync(string slug, bool includeUnpublished);

        Task<ContentItem> CreateAsync(SaveContentItemRequest request, string createdByUserId);

        Task<ContentItem> UpdateAsync(int id, SaveContentItemRequest request);

        Task<bool> DeleteAsync(int id);
    }
}
