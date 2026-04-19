using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<SiteUser> _userManager;

        public UsersController(UserManager<SiteUser> usrMgr)
        {
            _userManager = usrMgr;
        }

        // ── MVC actions (kept until React pages are cut over) ──────────────────

        [Authorize]
        public IActionResult Index()
        {
            return View(_userManager.Users);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Expanded()
        {
            return View(_userManager.Users);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                return View(user);
            else
                return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Update(string id, UserStatus status)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.Status != status)
                {
                    user.Status = status;
                    if (status == UserStatus.BETALT || status == UserStatus.STUDERENDE)
                    {
                        user.KontingentDato = DateTime.UtcNow;
                    }
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                        return RedirectToAction(nameof(Expanded));
                    ModelState.AddModelError("", "Status må ikke være tom");
                    Errors(result);
                }
            }
            else
            {
                ModelState.AddModelError("", "Bruger ikke fundet");
            }
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult UpdateMass()
        {
            return View(_userManager.Users);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMassEditAsync(DateTime from, DateTime to, UserStatus status)
        {
            var users = _userManager.Users.Where(it => it.KontingentDato >= from && it.KontingentDato <= to).ToList();
            foreach (var user in users.Where(user => user.Status != status))
            {
                user.Status = status;
                if (status == UserStatus.BETALT || status == UserStatus.STUDERENDE)
                {
                    user.KontingentDato = DateTime.UtcNow;
                }
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction(nameof(UpdateMass));
        }

        // ── REST API endpoints for the React SPA ───────────────────────────────

        // Member-facing list: active members only, visibility-gated contact details.
        [Authorize]
        [HttpGet("/api/users")]
        public IActionResult GetUsers()
        {
            var activeStatuses = new[] { UserStatus.BETALT, UserStatus.SKYLDER, UserStatus.STUDERENDE };
            var users = _userManager.Users
                .Where(u => activeStatuses.Contains(u.Status))
                .OrderBy(u => u.Navn)
                .Select(u => new
                {
                    id = u.Id,
                    navn = u.Navn,
                    aargang = u.Aargang,
                    beskaeftigelse = u.Visibility.IsVisible() ? u.Beskaeftigelse : null,
                    email = u.Visibility.IsVisible() ? u.Email : null,
                    phoneNumber = u.Visibility.IsVisible() ? u.PhoneNumber : null,
                })
                .ToList();

            return Ok(users);
        }

        // Admin list: all users, all fields, used to manage membership status.
        [Authorize(Roles = "Admin")]
        [HttpGet("/api/users/admin")]
        public IActionResult GetUsersAdmin()
        {
            var users = _userManager.Users
                .OrderBy(u => u.Navn)
                .Select(u => new
                {
                    id = u.Id,
                    navn = u.Navn,
                    aargang = u.Aargang,
                    beskaeftigelse = u.Beskaeftigelse,
                    status = u.Status.ToString(),
                    email = u.Email,
                    phoneNumber = u.PhoneNumber,
                    kontingentDato = u.KontingentDato,
                    oprettetDato = u.OprettetDato,
                })
                .ToList();

            return Ok(users);
        }

        // Update a single user's status. Admin only.
        [Authorize(Roles = "Admin")]
        [HttpPatch("/api/users/{id}/status")]
        public async Task<IActionResult> PatchStatus(string id, [FromBody] PatchStatusRequest body)
        {
            if (!Enum.TryParse<UserStatus>(body.Status, ignoreCase: true, out var newStatus))
                return BadRequest(new { error = "Ugyldigt status" });

            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (user.Status != newStatus)
            {
                user.Status = newStatus;
                if (newStatus == UserStatus.BETALT || newStatus == UserStatus.STUDERENDE)
                    user.KontingentDato = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors)
                        ModelState.AddModelError("", e.Description);
                    return ValidationProblem();
                }
            }

            return Ok(new
            {
                id = user.Id,
                status = user.Status.ToString(),
                kontingentDato = user.KontingentDato,
            });
        }

        // Bulk-update status for users whose KontingentDato falls within a date range. Admin only.
        [Authorize(Roles = "Admin")]
        [HttpPatch("/api/users/bulk-status")]
        public async Task<IActionResult> PatchBulkStatus([FromBody] BulkStatusRequest body)
        {
            if (!Enum.TryParse<UserStatus>(body.Status, ignoreCase: true, out var newStatus))
                return BadRequest(new { error = "Ugyldigt status" });

            var users = _userManager.Users
                .Where(u => u.KontingentDato >= body.From && u.KontingentDato <= body.To)
                .ToList();

            var updated = 0;
            foreach (var user in users.Where(u => u.Status != newStatus))
            {
                user.Status = newStatus;
                if (newStatus == UserStatus.BETALT || newStatus == UserStatus.STUDERENDE)
                    user.KontingentDato = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded) updated++;
            }

            return Ok(new { updated });
        }

        public record PatchStatusRequest(string Status);
        public record BulkStatusRequest(string Status, DateTime From, DateTime To);

        // ── Helpers ────────────────────────────────────────────────────────────

        private void Errors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}
