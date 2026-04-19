using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<SiteUser> _userManager;
        public RoleController(RoleManager<IdentityRole> roleMgr, UserManager<SiteUser> userMrg)
        {
            _roleManager = roleMgr;
            _userManager = userMrg;
        }

        public ViewResult Index() => View(_roleManager.Roles);

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Required(ErrorMessage = "{0} er obligatorisk")] string name)
        {
            if (ModelState.IsValid)
            {
                var role = new IdentityRole(name);
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    Errors(result);
            }
            return View(name);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                Errors(result);
            }
            else
                ModelState.AddModelError("", "No role found");
            return View("Index", _roleManager.Roles);
        }

        public async Task<IActionResult> Update(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            var members = new List<SiteUser>();
            var nonMembers = new List<SiteUser>();
            foreach (SiteUser user in _userManager.Users.ToList())
            {
                var list = await _userManager.IsInRoleAsync(user, role.Name) ? members : nonMembers;
                list.Add(user);
            }
            return View(new RoleEdit
            {
                Role = role,
                Members = members,
                NonMembers = nonMembers
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update(RoleModification model)
        {
            if (ModelState.IsValid)
            {
                IdentityResult result;
                foreach (var userId in model.AddIds ?? Array.Empty<string>())
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await _userManager.AddToRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }
                foreach (string userId in model.DeleteIds ?? new string[] { })
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }
            }

            if (ModelState.IsValid)
                return RedirectToAction(nameof(Index));
            else
                return await Update(model.RoleId);
        }

        // ── REST API endpoints for the React SPA ───────────────────────────────

        [HttpGet("/api/roles")]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new { id = r.Id, name = r.Name })
                .ToList();
            return Ok(roles);
        }

        [HttpPost("/api/roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest body)
        {
            if (string.IsNullOrWhiteSpace(body.Name))
                return BadRequest(new { error = "Rollenavn er obligatorisk" });

            var result = await _roleManager.CreateAsync(new IdentityRole(body.Name));
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return ValidationProblem();
            }

            var created = await _roleManager.FindByNameAsync(body.Name);
            return Ok(new { id = created!.Id, name = created.Name });
        }

        [HttpDelete("/api/roles/{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return ValidationProblem();
            }

            return Ok();
        }

        // Returns members and non-members for the role membership management UI.
        [HttpGet("/api/roles/{id}/members")]
        public async Task<IActionResult> GetRoleMembers(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            var members = new List<object>();
            var nonMembers = new List<object>();

            foreach (var user in _userManager.Users.OrderBy(u => u.Navn).ToList())
            {
                var entry = new { id = user.Id, navn = user.Navn, email = user.Email };
                if (await _userManager.IsInRoleAsync(user, role.Name!))
                    members.Add(entry);
                else
                    nonMembers.Add(entry);
            }

            return Ok(new { roleId = role.Id, roleName = role.Name, members, nonMembers });
        }

        // Batch add/remove users from a role.
        [HttpPatch("/api/roles/{id}/members")]
        public async Task<IActionResult> PatchRoleMembers(string id, [FromBody] PatchMembersRequest body)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            foreach (var userId in body.AddIds ?? [])
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is null) continue;
                var result = await _userManager.AddToRoleAsync(user, role.Name!);
                if (!result.Succeeded)
                    foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            }

            foreach (var userId in body.RemoveIds ?? [])
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is null) continue;
                var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
                if (!result.Succeeded)
                    foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            }

            if (!ModelState.IsValid)
                return ValidationProblem();

            return Ok();
        }

        public record CreateRoleRequest(string Name);
        public record PatchMembersRequest(string[]? AddIds, string[]? RemoveIds);

        // ── Helpers ────────────────────────────────────────────────────────────

        private void Errors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
        }
    }
}
