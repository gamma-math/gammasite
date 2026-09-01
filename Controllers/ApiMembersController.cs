using System;
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
    [Route("api/members")]
    [Authorize]
    public class ApiMembersController : ControllerBase
    {
        private const string AdminRoles = "Admin,ADMIN";
        private readonly UserManager<SiteUser> _userManager;

        public ApiMembersController(UserManager<SiteUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.Status == UserStatus.BETALT || user.Status == UserStatus.SKYLDER || user.Status == UserStatus.STUDERENDE)
                .OrderBy(user => user.Navn)
                .ToListAsync();

            return Ok(members.Select(user => ToMemberDto(user, user.Visibility.IsVisible())));
        }

        [HttpGet("admin")]
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> GetAdminMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.Navn)
                .ToListAsync();

            return Ok(members.Select(user => ToMemberDto(user, true)));
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> UpdateStatus(string id, UpdateMemberStatusRequest request)
        {
            if (!Enum.TryParse<UserStatus>(request?.Status, true, out var status))
            {
                return BadRequest(new { error = "Status er ugyldig" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = status;
            if (status == UserStatus.BETALT || status == UserStatus.STUDERENDE)
            {
                user.KontingentDato = DateTime.UtcNow;
            }

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? Ok(ToMemberDto(user, true)) : BadRequest(new { error = string.Join(", ", result.Errors.Select(error => error.Description)) });
        }

        [HttpPost("admin/mass-status")]
        [Authorize(Roles = AdminRoles)]
        public async Task<IActionResult> UpdateMassStatus(MassUpdateMemberStatusRequest request)
        {
            if (!Enum.TryParse<UserStatus>(request?.Status, true, out var status))
            {
                return BadRequest(new { error = "Status er ugyldig" });
            }

            var users = await _userManager.Users
                .Where(user => user.KontingentDato >= request.From && user.KontingentDato <= request.To)
                .ToListAsync();

            var updated = 0;
            foreach (var user in users.Where(user => user.Status != status))
            {
                user.Status = status;
                if (status == UserStatus.BETALT || status == UserStatus.STUDERENDE)
                {
                    user.KontingentDato = DateTime.UtcNow;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    updated++;
                }
            }

            return Ok(new MassUpdateMemberStatusResult { Updated = updated });
        }

        private static MemberDto ToMemberDto(SiteUser user, bool includePrivate)
        {
            return new MemberDto
            {
                Id = user.Id,
                Name = user.Navn,
                GraduationYear = user.Aargang,
                Occupation = includePrivate ? user.Beskaeftigelse : null,
                Email = includePrivate ? user.Email : null,
                PhoneNumber = includePrivate ? user.PhoneNumber : null,
                Status = user.Status.ToString(),
                IsVisible = user.Visibility.IsVisible(),
                MembershipPaidAt = user.KontingentDato,
                CreatedAt = user.OprettetDato
            };
        }
    }
}
