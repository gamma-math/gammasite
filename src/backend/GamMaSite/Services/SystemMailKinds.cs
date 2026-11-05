using System;

namespace GamMaSite.Services
{
    public static class SystemMailKinds
    {
        public static bool IsSystem(string templateType)
        {
            var normalizedType = templateType?.Trim();
            return !string.IsNullOrWhiteSpace(normalizedType) &&
                normalizedType.StartsWith("SYSTEM_", StringComparison.OrdinalIgnoreCase);
        }

        public static bool RequiresActiveTemplate(string templateType)
        {
            var normalizedType = templateType?.Trim();
            return string.Equals(normalizedType, SystemEmailTemplateService.RegistrationConfirmation, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, SystemEmailTemplateService.EmailConfirmation, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, SystemEmailTemplateService.EmailChangeConfirmation, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, SystemEmailTemplateService.PasswordReset, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, SystemEmailTemplateService.FinancePaymentReceipt, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, SystemEmailTemplateService.FinanceRefundReceipt, StringComparison.OrdinalIgnoreCase);
        }
    }
}
