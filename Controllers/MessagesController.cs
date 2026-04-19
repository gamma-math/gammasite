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

        // ── MVC actions (kept until React page is cut over) ───────────────────

        public IActionResult Index()
        {
            var userCategories = new UserCategories(_roleManager, _userManager);
            return View(userCategories);
        }

        public async Task<IActionResult> Send(UserStatus[] status, string role, MessageMedia media, string subject, string messageBody, string smsBody)
        {
            var usersToReceiveMessage = new HashSet<SiteUser>();

            if (status?.Length > 0)
            {
                var selectedStatuses = status.ToHashSet();
                usersToReceiveMessage.UnionWith(
                    _userManager.Users
                        .AsEnumerable()
                        .Where(user => selectedStatuses.Contains(user.Status))
                );
            }

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

        // ── REST API endpoints for the React SPA ───────────────────────────────

        // Returns available statuses and roles to populate the recipient selectors.
        [HttpGet("/api/messages/recipients")]
        public IActionResult GetRecipients()
        {
            var statuses = _userManager.Users
                .Select(u => u.Status)
                .Distinct()
                .OrderBy(s => s)
                .Select(s => s.ToString())
                .ToList();

            var roles = _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name)
                .ToList();

            return Ok(new { statuses, roles });
        }

        [HttpPost("/api/messages/send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest body)
        {
            if (string.IsNullOrWhiteSpace(body.Subject))
                return BadRequest(new { error = "Emne er obligatorisk" });

            if ((body.Media == "Email" || body.Media == "EmailSMS") && string.IsNullOrWhiteSpace(body.MessageBody))
                return BadRequest(new { error = "Beskedtekst er obligatorisk for e-mail" });

            if ((body.Media == "SMS" || body.Media == "EmailSMS") && string.IsNullOrWhiteSpace(body.SmsBody))
                return BadRequest(new { error = "SMS-tekst er obligatorisk" });

            if (!Enum.TryParse<MessageMedia>(body.Media, out var media))
                return BadRequest(new { error = "Ugyldigt medie" });

            var statuses = (body.Statuses ?? [])
                .Select(s => Enum.TryParse<UserStatus>(s, out var us) ? (UserStatus?)us : null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToArray();

            var usersToReceive = _userManager.Users
                .Where(u => statuses.Contains(u.Status))
                .ToHashSet();

            if (!string.IsNullOrEmpty(body.Role))
                usersToReceive.UnionWith(await _userManager.GetUsersInRoleAsync(body.Role));

            foreach (var role in body.Roles ?? [])
                usersToReceive.UnionWith(await _userManager.GetUsersInRoleAsync(role));

            if (media == MessageMedia.SMS || media == MessageMedia.EmailSMS)
            {
                var phones = usersToReceive
                    .Where(u => !string.IsNullOrEmpty(u.PhoneNumber))
                    .Select(u => u.PhoneNumber!)
                    .ToArray();
                await _smsSender.SendSmsAsync(body.SmsBody!, phones);
            }

            if (media == MessageMedia.Email || media == MessageMedia.EmailSMS)
            {
                var mails = usersToReceive
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email!)
                    .ToArray();
                await _emailService.SendEmailAsync(mails, body.Subject, body.MessageBody!);
            }

            return Ok(new { sent = usersToReceive.Count });
        }

        public record SendMessageRequest(
            string[] Statuses,
            string? Role,
            string[]? Roles,
            string Media,
            string Subject,
            string? MessageBody,
            string? SmsBody
        );
    }
}
