using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/me")]
    /*
     * Returns the current authentication and role state for the React application.
     */
    public class ApiMeController : ControllerBase
    {
        private readonly UserManager<SiteUser> _userManager;
        private readonly IAccessControlService _accessControl;

        public ApiMeController(UserManager<SiteUser> userManager, IAccessControlService accessControl)
        {
            _userManager = userManager;
            _accessControl = accessControl;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurrentUser()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Ok(new
                {
                    isAuthenticated = false,
                    roles = new string[] { },
                    permissions = new string[] { }
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Ok(new
                {
                    isAuthenticated = false,
                    roles = new string[] { },
                    permissions = new string[] { }
                });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _accessControl.GetPermissionsAsync(User);
            return Ok(new
            {
                isAuthenticated = true,
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                name = user.Navn,
                roles = roles.ToArray(),
                permissions
            });
        }
    }
}
