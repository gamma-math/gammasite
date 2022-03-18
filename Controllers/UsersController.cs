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
                    if (status == UserStatus.BETALT)
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
                var result = await _userManager.UpdateAsync(user);
            }
            return RedirectToAction(nameof(UpdateMass));
        }

        private void Errors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}
