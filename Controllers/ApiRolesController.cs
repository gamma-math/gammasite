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
    [Route("api/roles")]
    [Authorize(Roles = "Admin,ADMIN")]
    public class ApiRolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<SiteUser> _userManager;

        public ApiRolesController(RoleManager<IdentityRole> roleManager, UserManager<SiteUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.OrderBy(role => role.Name).ToListAsync();
            return Ok(roles.Select(ToDto));
        }

        [HttpPost]
        public async Task<IActionResult> Create(SaveRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
            {
                return BadRequest(new { error = "Navn er obligatorisk" });
            }

            var role = new IdentityRole(request.Name.Trim());
            var result = await _roleManager.CreateAsync(role);
            return result.Succeeded ? Ok(ToDto(role)) : BadRequest(new { error = string.Join(", ", result.Errors.Select(error => error.Description)) });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            if (string.Equals(role.Name, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "ADMIN-rollen kan ikke slettes" });
            }

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded ? NoContent() : BadRequest(new { error = string.Join(", ", result.Errors.Select(error => error.Description)) });
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var users = await _userManager.Users.OrderBy(user => user.Navn).ToListAsync();
            var members = new System.Collections.Generic.List<MemberDto>();
            var nonMembers = new System.Collections.Generic.List<MemberDto>();

            foreach (var user in users)
            {
                var target = await _userManager.IsInRoleAsync(user, role.Name) ? members : nonMembers;
                target.Add(ToMemberDto(user));
            }

            return Ok(new RoleMembersDto { Role = ToDto(role), Members = members, NonMembers = nonMembers });
        }

        [HttpPut("{id}/members")]
        public async Task<IActionResult> UpdateMembers(string id, UpdateRoleMembersRequest request)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            foreach (var userId in request?.AddIds ?? Array.Empty<string>())
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !await _userManager.IsInRoleAsync(user, role.Name))
                {
                    await _userManager.AddToRoleAsync(user, role.Name);
                }
            }

            foreach (var userId in request?.DeleteIds ?? Array.Empty<string>())
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && await _userManager.IsInRoleAsync(user, role.Name))
                {
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
                }
            }

            return await GetMembers(id);
        }

        private static RoleDto ToDto(IdentityRole role)
        {
            return new RoleDto { Id = role.Id, Name = role.Name };
        }

        private static MemberDto ToMemberDto(SiteUser user)
        {
            return new MemberDto
            {
                Id = user.Id,
                Name = user.Navn,
                GraduationYear = user.Aargang,
                Occupation = user.Beskaeftigelse,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Status = user.Status.ToString(),
                IsVisible = user.Visibility.IsVisible(),
                MembershipPaidAt = user.KontingentDato,
                CreatedAt = user.OprettetDato
            };
        }
    }
}
