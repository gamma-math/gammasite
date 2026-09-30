using System.Threading.Tasks;

namespace GamMaSite.Services
{
    /*
     * Defines rendering for system-owned email templates.
     */
    public interface ISystemEmailTemplateService
    {
        Task SendRegistrationConfirmationAsync(string email, string name, string confirmationUrl);

        Task SendEmailConfirmationAsync(string email, string name, string confirmationUrl);

        Task SendEmailChangeConfirmationAsync(string email, string name, string confirmationUrl);

        Task SendPasswordResetAsync(string email, string name, string resetUrl);
    }
}
