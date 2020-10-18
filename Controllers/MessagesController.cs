using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Areas.Identity.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GamMaSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MessagesController : Controller
    {
        private RoleManager<IdentityRole> roleManager;
        private UserManager<GamMaUser> userManager;
        private ISmsSender smsSender;
        private IEmailSender emailSender;
        public MessagesController(RoleManager<IdentityRole> roleMgr, UserManager<GamMaUser> userMrg, ISmsSender smsSdr, IEmailSender emailSdr)
        {
            roleManager = roleMgr;
            userManager = userMrg;
            smsSender = smsSdr;
            emailSender = emailSdr;
        }

        public IActionResult Index()
        {
            return View(userManager.Users.Select(user => user.Status).Distinct().ToList());
        }

        public async Task<IActionResult> Send(UserStatus[] status, string role, string messageBody, MessageMedia media, string subject)
        {
            var usersToReceiveMessage = userManager.Users.Where(user => status.Contains(user.Status)).ToHashSet();
            usersToReceiveMessage.UnionWith(await userManager.GetUsersInRoleAsync(role));
            if (media == MessageMedia.SMS || media == MessageMedia.EmailSMS)
            {
                var userPhonenumbers = usersToReceiveMessage.Where(user => !String.IsNullOrEmpty(user.PhoneNumber)).Select(user => user.PhoneNumber).ToArray();
                var result = await smsSender.SendSmsAsync(messageBody, userPhonenumbers);
            }
            if (media == MessageMedia.Email || media == MessageMedia.EmailSMS)
            {
                var usersMails = usersToReceiveMessage.Where(user => !String.IsNullOrEmpty(user.Email)).Select(user => user.Email).ToArray();
                foreach (var mail in usersMails)
                {
                    await emailSender.SendEmailAsync(mail, !String.IsNullOrEmpty(subject) ? subject : "", messageBody);
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
