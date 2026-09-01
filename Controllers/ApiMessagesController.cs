using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize(Roles = "Admin,ADMIN")]
    public class ApiMessagesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<SiteUser> _userManager;

        public ApiMessagesController(RoleManager<IdentityRole> roleManager, UserManager<SiteUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var statuses = await _userManager.Users
                .Select(user => user.Status)
                .Distinct()
                .ToListAsync();
            var roles = await _roleManager.Roles
                .OrderBy(role => role.Name)
                .Select(role => role.Name)
                .ToListAsync();

            return Ok(new MessageCategoriesDto
            {
                Statuses = statuses.Select(status => status.ToString()).OrderBy(status => status).ToList(),
                Roles = roles
            });
        }

        [HttpPost("recipient-preview")]
        public async Task<IActionResult> PreviewRecipients(MessageRecipientPreviewRequest request)
        {
            var recipients = new HashSet<SiteUser>();
            var requestedStatuses = (request?.Statuses ?? System.Array.Empty<string>())
                .Select(status => System.Enum.TryParse<UserStatus>(status, true, out var parsed) ? parsed : (UserStatus?)null)
                .Where(status => status.HasValue)
                .Select(status => status.Value)
                .ToHashSet();

            if (requestedStatuses.Count > 0)
            {
                recipients.UnionWith(await _userManager.Users.Where(user => requestedStatuses.Contains(user.Status)).ToListAsync());
            }

            foreach (var role in request?.Roles ?? System.Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    recipients.UnionWith(await _userManager.GetUsersInRoleAsync(role));
                }
            }

            return Ok(new MessageRecipientPreviewDto
            {
                RecipientCount = recipients.Count,
                EmailCount = recipients.Count(user => !string.IsNullOrWhiteSpace(user.Email)),
                SmsCount = recipients.Count(user => !string.IsNullOrWhiteSpace(user.PhoneNumber))
            });
        }
    }
}
