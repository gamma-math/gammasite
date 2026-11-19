using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/contact")]
    [AutoValidateAntiforgeryToken]
    public class ApiContactController : ControllerBase
    {
        private const string ContactRoleName = "Kontakt Ansvarlig";
        private readonly UserManager<SiteUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ISystemEmailTemplateService _systemEmailTemplateService;

        public ApiContactController(
            UserManager<SiteUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ISystemEmailTemplateService systemEmailTemplateService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _systemEmailTemplateService = systemEmailTemplateService;
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Send(ContactRequestDto request)
        {
            var role = await _roleManager.FindByNameAsync(ContactRoleName);
            var recipients = role == null
                ? Array.Empty<string>()
                : (await _userManager.GetUsersInRoleAsync(role.Name))
                    .Select(user => user.Email)
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            if (recipients.Length == 0)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                recipients = admins.Select(user => user.Email)
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            if (recipients.Length == 0)
            {
                return StatusCode(503, new { error = "Der er ingen kontaktmodtagere konfigureret." });
            }

            try
            {
                var requestSent = await _systemEmailTemplateService.SendContactRequestAsync(recipients, request.ContactType, request.Topic, request.Email, request.Message);
                if (!requestSent)
                {
                    return Ok(new { message = "Kontaktformularen er midlertidigt deaktiveret." });
                }

                var confirmationSent = await _systemEmailTemplateService.SendContactConfirmationAsync(request.Email, request.ContactType, request.Topic, request.Message);
                return Ok(new
                {
                    message = confirmationSent
                        ? "Tak for din henvendelse. Vi har sendt en bekræftelse til din e-mail."
                        : "Tak for din henvendelse. Bekræftelsesmailen er midlertidigt deaktiveret."
                });
            }
            catch (Exception)
            {
                return StatusCode(502, new { error = "Henvendelsen kunne ikke sendes lige nu. Prøv igen senere." });
            }
        }
    }
}
