using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Services
{
    public class SystemEmailTemplateService : ISystemEmailTemplateService
    {
        public const string RegistrationConfirmation = "SYSTEM_REGISTRATION_CONFIRMATION";
        public const string EmailConfirmation = "SYSTEM_EMAIL_CONFIRMATION";
        public const string EmailChangeConfirmation = "SYSTEM_EMAIL_CHANGE_CONFIRMATION";
        public const string PasswordReset = "SYSTEM_PASSWORD_RESET";

        private static readonly Regex PlaceholderRegex = new(@"\{\{([A-Za-z0-9_]+)\}\}", RegexOptions.Compiled);

        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public SystemEmailTemplateService(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public Task SendRegistrationConfirmationAsync(string email, string name, string confirmationUrl)
        {
            return SendAsync(
                RegistrationConfirmation,
                email,
                name,
                confirmationUrl,
                "Bekræft din email",
                "Bekræft din GamMa-bruger",
                "Bekræft venligst din GamMa-bruger ved at klikke på knappen herunder.",
                "For at blive godkendt som medlem, kan du kontakte foreningens bestyrelse på bestyrelsen@gam-ma.dk. Gør i den forbindelse opmærksom på, om du er studerende eller har færdiggjort dine studier.",
                "Bekræft bruger");
        }

        public Task SendEmailConfirmationAsync(string email, string name, string confirmationUrl)
        {
            return SendAsync(
                EmailConfirmation,
                email,
                name,
                confirmationUrl,
                "Bekræft din email",
                "Bekræft din email",
                "Bekræft venligst din profil ved at klikke på knappen herunder.",
                "Hvis du ikke har bedt om denne mail, kan du roligt ignorere den.",
                "Bekræft email");
        }

        public Task SendEmailChangeConfirmationAsync(string email, string name, string confirmationUrl)
        {
            return SendAsync(
                EmailChangeConfirmation,
                email,
                name,
                confirmationUrl,
                "Bekræft email",
                "Bekræft din nye email",
                "Bekræft venligst din nye email ved at klikke på knappen herunder.",
                "Din email bliver først ændret, når du har bekræftet den.",
                "Bekræft ny email");
        }

        public Task SendPasswordResetAsync(string email, string name, string resetUrl)
        {
            return SendAsync(
                PasswordReset,
                email,
                name,
                resetUrl,
                "Nulstil Password",
                "Nulstil dit password",
                "Nulstil venligst dit kodeord ved at klikke på knappen herunder.",
                "Hvis du ikke har bedt om at nulstille dit password, kan du roligt ignorere denne mail.",
                "Nulstil password");
        }

        private async Task SendAsync(
            string templateType,
            string email,
            string name,
            string actionUrl,
            string fallbackSubject,
            string fallbackHeading,
            string fallbackIntro,
            string fallbackNote,
            string fallbackActionText)
        {
            var template = await GetActiveTemplate(templateType);
            var rawValues = BuildRawValues(name, email, actionUrl, fallbackHeading, fallbackIntro, fallbackNote, fallbackActionText);
            var htmlValues = rawValues.ToDictionary(pair => pair.Key, pair => HtmlEncoder.Default.Encode(pair.Value ?? string.Empty));

            var subject = template == null
                ? fallbackSubject
                : Render(template.Subject, rawValues);

            var html = template == null
                ? BuildFallbackHtml(htmlValues)
                : Render(template.HtmlBody, htmlValues);

            await _emailService.SendEmailAsync(email, CleanSubject(subject), html);
        }

        private async Task<EmailTemplate> GetActiveTemplate(string templateType)
        {
            return await _db.EmailTemplates
                .AsNoTracking()
                .Where(template => template.TemplateType == templateType && template.IsActive)
                .OrderByDescending(template => template.Updated)
                .ThenByDescending(template => template.Id)
                .FirstOrDefaultAsync();
        }

        private static Dictionary<string, string> BuildRawValues(
            string name,
            string email,
            string actionUrl,
            string heading,
            string intro,
            string note,
            string actionText)
        {
            return new Dictionary<string, string>
            {
                ["Name"] = string.IsNullOrWhiteSpace(name) ? "GamMa-medlem" : name.Trim(),
                ["Email"] = email ?? string.Empty,
                ["ActionUrl"] = actionUrl ?? string.Empty,
                ["ConfirmationUrl"] = actionUrl ?? string.Empty,
                ["ResetUrl"] = actionUrl ?? string.Empty,
                ["Heading"] = heading,
                ["Intro"] = intro,
                ["Note"] = note,
                ["ActionText"] = actionText
            };
        }

        private static string Render(string template, Dictionary<string, string> values)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return string.Empty;
            }

            return PlaceholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return values.TryGetValue(key, out var value) ? value ?? string.Empty : match.Value;
            });
        }

        private static string CleanSubject(string subject)
        {
            return (subject ?? string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();
        }

        private static string BuildFallbackHtml(Dictionary<string, string> values)
        {
            return $@"<div style=""margin:0;padding:0;background-color:#eaf2fb;width:100%;font-family:Arial,sans-serif;color:#173042;"">
  <a href=""https://gam-ma.dk/"" target=""_blank"" style=""text-decoration:none;display:block;"">
    <img src=""https://gam-ma.dk/lib/logo_blue.png"" alt=""GamMa"" width=""100%"" style=""display:block;border:0;outline:none;text-decoration:none;height:auto;max-height:180px;object-fit:contain;background-color:#ffffff;padding:16px 0;"">
  </a>
  <div style=""width:90%;max-width:640px;margin:18px auto 22px auto;background-color:#ffffff;padding:28px 24px;line-height:1.55;font-size:15px;border:1px solid #d9e7f5;"">
    <p style=""margin:0 0 12px;"">Hej <strong>{values["Name"]}</strong></p>
    <h1 style=""margin:0 0 14px;font-size:28px;line-height:1.15;color:#10233a;"">{values["Heading"]}</h1>
    <p style=""margin:0 0 18px;"">{values["Intro"]}</p>
    <a href=""{values["ActionUrl"]}"" target=""_blank"" style=""display:block;width:100%;box-sizing:border-box;text-align:center;background-color:#2485c7;color:#ffffff;text-decoration:none;padding:13px 18px;font-size:15px;font-weight:bold;"">{values["ActionText"]}</a>
    <p style=""margin:18px 0 0;color:#4f6475;font-size:14px;"">{values["Note"]}</p>
    <p style=""margin:18px 0 0;"">Med venlig hilsen<br><strong>GamMas bestyrelse</strong></p>
  </div>
  <div style=""height:1px;background-color:#cfe0ef;width:90%;max-width:640px;margin:0 auto 12px auto;""></div>
  <div style=""width:90%;max-width:640px;margin:0 auto 24px auto;text-align:center;background-color:#ffffff;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;border:none;background:none;""><tbody><tr>
      <td style=""padding:10px;border:none;background:none;text-align:center;""><a href=""https://www.linkedin.com/company/gamma-math-ucph/"" target=""_blank"" style=""text-decoration:none;display:inline-block;text-align:center;""><img src=""https://cdn-icons-png.flaticon.com/512/174/174857.png"" alt=""LinkedIn"" width=""36"" style=""display:block;margin:0 auto;border:0;height:auto;""><div style=""margin-top:6px;font-size:12px;color:#000000;text-align:center;"">LinkedIn</div></a></td>
      <td style=""padding:10px;border:none;background:none;text-align:center;""><a href=""https://www.instagram.com/gamma_ku_2100?igsh=MTFjYmk4M213djc0NA=="" target=""_blank"" style=""text-decoration:none;display:inline-block;text-align:center;""><img src=""https://cdn-icons-png.flaticon.com/512/2111/2111463.png"" alt=""Instagram"" width=""36"" style=""display:block;margin:0 auto;border:0;height:auto;""><div style=""margin-top:6px;font-size:12px;color:#000000;text-align:center;"">Instagram</div></a></td>
      <td style=""padding:10px;border:none;background:none;text-align:center;""><a href=""https://www.facebook.com/share/g/1Exv7epudc/?mibextid=wwXIfr"" target=""_blank"" style=""text-decoration:none;display:inline-block;text-align:center;""><img src=""https://cdn-icons-png.flaticon.com/512/733/733547.png"" alt=""Facebook"" width=""36"" style=""display:block;margin:0 auto;border:0;height:auto;""><div style=""margin-top:6px;font-size:12px;color:#000000;text-align:center;"">Facebook</div></a></td>
    </tr></tbody></table>
  </div>
  <div style=""height:16px;""></div>
</div>";
        }
    }
}
