using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;

namespace GamMaSite.Services
{
    /*
     * Defines email template operations used by the React admin template flow.
     */
    public interface IEmailTemplateService
    {
        Task<IReadOnlyList<EmailTemplate>> GetAllAsync(string templateType, bool? isActive);

        Task<EmailTemplate> GetByIdAsync(int id);

        Task<EmailTemplate> CreateAsync(SaveEmailTemplateRequest request);

        Task<EmailTemplate> UpdateAsync(int id, SaveEmailTemplateRequest request);

        Task<bool> DeleteAsync(int id);

        Task<EmailTemplatePreviewDto> PreviewAsync(int id, PreviewEmailTemplateRequest request);
    }
}
