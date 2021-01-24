﻿using System;
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
        private UserManager<GamMaUser> userManager;

        public UsersController(UserManager<GamMaUser> usrMgr)
        {
            userManager = usrMgr;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View(userManager.Users);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Expanded()
        {
            return View(userManager.Users);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string id)
        {
            GamMaUser user = await userManager.FindByIdAsync(id);
            if (user != null)
                return View(user);
            else
                return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Update(string id, UserStatus status)
        {
            GamMaUser user = await userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.Status != status)
                {
                    user.Status = status;
                    if (status == UserStatus.BETALT)
                    {
                        user.KontingentDato = DateTime.Now;
                    }
                    IdentityResult result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                        return RedirectToAction(nameof(Expanded));
                    else
                    {
                        ModelState.AddModelError("", "Status må ikke være tom");
                        Errors(result);
                    }
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
            return View(userManager.Users);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMassEditAsync(DateTime from, DateTime to, UserStatus status)
        {
            var users = userManager.Users.Where(it => it.KontingentDato >= from && it.KontingentDato <= to).ToList();
            foreach(GamMaUser user in users)
            {
                if (user.Status != status)
                {
                    user.Status = status;
                    if (status == UserStatus.BETALT)
                    {
                        user.KontingentDato = DateTime.Now;
                    }
                    IdentityResult result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                        return RedirectToAction(nameof(Expanded));
                    else
                    {
                        ModelState.AddModelError("", "Status må ikke være tom");
                        Errors(result);
                    }
                }
            }
            return RedirectToAction(nameof(UpdateMass));
        }

        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}
