using System;
using System.Threading.Tasks;
using GamMaSite.Models;

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

        Task SendPaymentConfirmationAsync(
            string email,
            string name,
            string productName,
            long amountMinor,
            string currency,
            string paymentProvider,
            DateTime paymentDate,
            bool recurring,
            string billingInterval,
            int? billingIntervalCount,
            string subscriptionUrl);

        Task SendEventConfirmationAsync(string email, string name, ContentItem content, string registrationType);

        Task SendWaitlistPromotionAsync(string email, string name, ContentItem content);
    }
}
