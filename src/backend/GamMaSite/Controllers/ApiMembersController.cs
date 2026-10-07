using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
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
    [AutoValidateAntiforgeryToken]
    /*
     * Provides React endpoints for the member directory and admin membership updates.
     */
    public class ApiMembersController : ControllerBase
    {
        private readonly UserManager<SiteUser> _userManager;
        private readonly IAccessControlService _accessControl;

        public ApiMembersController(UserManager<SiteUser> userManager, IAccessControlService accessControl)
        {
            _userManager = userManager;
            _accessControl = accessControl;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.Status != UserStatus.INAKTIV && user.Status != UserStatus.OPRETTET && user.EmailConfirmed)
                .OrderBy(user => user.Navn)
                .ToListAsync();

            return Ok(members.Select(user => ToMemberDto(user, user.Visibility.IsVisible())));
        }

        [HttpGet("admin")]
        [Authorize(Policy = PermissionPolicies.MemberData)]
        public async Task<IActionResult> GetAdminMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.Navn)
                .ToListAsync();

            return Ok(members.Select(user => ToMemberDto(user, true)));
        }

        [HttpGet("finance")]
        [Authorize(Policy = PermissionPolicies.FinanceMemberData)]
        public async Task<IActionResult> GetFinanceMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.Navn)
                .Select(user => new FinanceMemberDto
                {
                    Id = user.Id,
                    Name = user.Navn
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpGet("event/{contentItemId:int}")]
        public async Task<IActionResult> GetEventMembers(int contentItemId)
        {
            if (!await _accessControl.CanEditRegistrationsAsync(User, contentItemId))
            {
                return Forbid();
            }

            var members = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.Navn)
                .ToListAsync();

            return Ok(members.Select(user => ToMemberDto(user, true)));
        }

        [HttpPut("{id}/status")]
        [Authorize(Policy = PermissionPolicies.RolesEdit)]
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
        [Authorize(Policy = PermissionPolicies.RolesEdit)]
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
