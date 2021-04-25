using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private UserManager<SiteUser> userManager;
        private ISmsSender smsSender;
        private IEmailSender emailSender;
        public MessagesController(RoleManager<IdentityRole> roleMgr, UserManager<SiteUser> userMrg, ISmsSender smsSdr, IEmailSender emailSdr)
        {
            roleManager = roleMgr;
            userManager = userMrg;
            smsSender = smsSdr;
            emailSender = emailSdr;
        }

        public IActionResult Index()
        {
            var userCategories = new UserCategories(roleManager, userManager);
            return View(userCategories);
        }

        public async Task<IActionResult> Send(UserStatus[] status, string role, MessageMedia media, string subject, string messageBody, string smsBody)
        {
            var usersToReceiveMessage = userManager.Users.Where(user => status.Contains(user.Status)).ToHashSet();
            if (!String.IsNullOrEmpty(role))
            {
                usersToReceiveMessage.UnionWith(await userManager.GetUsersInRoleAsync(role));
            }
            if (media == MessageMedia.SMS || media == MessageMedia.EmailSMS)
            {
                var userPhonenumbers = usersToReceiveMessage.Where(user => !string.IsNullOrEmpty(user?.PhoneNumber)).Select(user => user.PhoneNumber).ToArray();
                var result = await smsSender.SendSmsAsync(smsBody, userPhonenumbers);
            }
            if (media == MessageMedia.Email || media == MessageMedia.EmailSMS)
            {
                var usersMails = usersToReceiveMessage.Where(user => !string.IsNullOrEmpty(user?.Email)).Select(user => user.Email).ToArray();

                await Task.WhenAll(usersMails.Select(mail => MailTask(mail, subject, messageBody)));
            }
            return RedirectToAction(nameof(Index));
        }

        private Task MailTask(string mail, string subject, string messageBody)
        {
            return emailSender.SendEmailAsync(mail, !string.IsNullOrEmpty(subject) ? subject : "", messageBody);
        }
    }
}
