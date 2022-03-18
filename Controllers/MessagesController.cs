using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MessagesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<SiteUser> _userManager;
        private readonly ISmsSender _smsSender;
        private readonly IEmailService _emailService;
        public MessagesController(RoleManager<IdentityRole> roleMgr, UserManager<SiteUser> userMrg, ISmsSender smsSdr, IEmailService emailService)
        {
            _roleManager = roleMgr;
            _userManager = userMrg;
            _smsSender = smsSdr;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            var userCategories = new UserCategories(_roleManager, _userManager);
            return View(userCategories);
        }

        public async Task<IActionResult> Send(UserStatus[] status, string role, MessageMedia media, string subject, string messageBody, string smsBody)
        {
            var usersToReceiveMessage = _userManager.Users.Where(user => status.Contains(user.Status)).ToHashSet();
            if (!String.IsNullOrEmpty(role))
            {
                usersToReceiveMessage.UnionWith(await _userManager.GetUsersInRoleAsync(role));
            }
            if (media == MessageMedia.SMS || media == MessageMedia.EmailSMS)
            {
                var userPhonenumbers = usersToReceiveMessage.Where(user => !string.IsNullOrEmpty(user?.PhoneNumber)).Select(user => user.PhoneNumber).ToArray();
                var result = await _smsSender.SendSmsAsync(smsBody, userPhonenumbers);
            }
            if (media == MessageMedia.Email || media == MessageMedia.EmailSMS)
            {
                var mails = usersToReceiveMessage.Where(user => !string.IsNullOrEmpty(user?.Email)).Select(user => user.Email).ToArray();
                await _emailService.SendEmailAsync(mails, subject, messageBody);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
