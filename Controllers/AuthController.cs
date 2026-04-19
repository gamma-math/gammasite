using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<SiteUser> _userManager;

        public AuthController(UserManager<SiteUser> userManager)
        {
            _userManager = userManager;
        }

        // Returns the currently authenticated user's identity and roles.
        // Roles are the caller's own roles, used by the SPA for UI rendering only.
        // Authorization is always enforced independently server-side on each endpoint.
        // Returns 401 if the caller is not authenticated.
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                name = user.Navn,
                status = user.Status.ToString(),
                roles = roles.ToArray(),
            });
        }
    }
}
