using System;

namespace GamMaSite.Services
{
    public static class SystemMailKinds
    {
        public static bool IsSystem(string templateType) =>
            !string.IsNullOrWhiteSpace(templateType) &&
            templateType.StartsWith("SYSTEM_", StringComparison.OrdinalIgnoreCase);

        public static bool RequiresActiveTemplate(string templateType) =>
            string.Equals(templateType, SystemEmailTemplateService.RegistrationConfirmation, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(templateType, SystemEmailTemplateService.EmailConfirmation, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(templateType, SystemEmailTemplateService.EmailChangeConfirmation, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(templateType, SystemEmailTemplateService.PasswordReset, StringComparison.OrdinalIgnoreCase);
    }
}
