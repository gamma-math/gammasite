using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Data;
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
    [AutoValidateAntiforgeryToken]
    /*
     * Provides React admin endpoints for role listing, editing, and membership assignment.
     */
    public class ApiRolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<SiteUser> _userManager;
        private readonly ApplicationDbContext _db;

        public ApiRolesController(
            RoleManager<IdentityRole> roleManager,
            UserManager<SiteUser> userManager,
            ApplicationDbContext db = null)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
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

        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetPermissions(string id)
        {
            if (_db == null)
            {
                return StatusCode(500, new { error = "Permission-databasen er ikke tilgængelig." });
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var enabledCodes = await _db.RolePermissions
                .Where(item => item.RoleId == id)
                .Select(item => item.Permission.Code)
                .ToListAsync();
            var enabled = new HashSet<string>(enabledCodes, StringComparer.OrdinalIgnoreCase);

            var permissions = await _db.Permissions
                .AsNoTracking()
                .OrderBy(item => item.Code)
                .ToListAsync();

            return Ok(new RolePermissionsDto
            {
                Role = ToDto(role),
                Permissions = permissions.Select(permission => new PermissionDto
                {
                    Id = permission.Id,
                    Code = permission.Code,
                    Description = permission.Description,
                    Enabled = enabled.Contains(permission.Code)
                }).ToList()
            });
        }

        [HttpPut("{id}/permissions")]
        public async Task<IActionResult> UpdatePermissions(string id, UpdateRolePermissionsRequest request)
        {
            if (_db == null)
            {
                return StatusCode(500, new { error = "Permission-databasen er ikke tilgængelig." });
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var requestedCodes = new HashSet<string>(
                request?.PermissionCodes ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            var permissions = await _db.Permissions.ToListAsync();
            var selected = permissions
                .Where(permission => requestedCodes.Contains(permission.Code))
                .ToList();

            var current = await _db.RolePermissions
                .Where(item => item.RoleId == id)
                .ToListAsync();
            _db.RolePermissions.RemoveRange(current);
            _db.RolePermissions.AddRange(selected.Select(permission => new RolePermission
            {
                RoleId = id,
                PermissionId = permission.Id
            }));
            await _db.SaveChangesAsync();

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in existingClaims.Where(claim => claim.Type == "permission").ToList())
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var permission in selected)
            {
                await _roleManager.AddClaimAsync(role, new Claim("permission", permission.Code));
            }

            return await GetPermissions(id);
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
