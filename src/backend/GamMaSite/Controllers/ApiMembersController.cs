using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/members")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    /*
     * Provides React endpoints for the member directory and admin membership updates.
     */
    public class ApiMembersController : ControllerBase
    {
        private readonly UserManager<SiteUser> _userManager;
        private readonly IAccessControlService _accessControl;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public ApiMembersController(UserManager<SiteUser> userManager, IAccessControlService accessControl, ApplicationDbContext db, IConfiguration configuration = null)
        {
            _userManager = userManager;
            _accessControl = accessControl;
            _db = db;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.Status != UserStatus.INAKTIV && user.Status != UserStatus.OPRETTET && user.EmailConfirmed)
                .OrderBy(user => user.Navn)
                .ToListAsync();

            return Ok(members.Select(user => ToMemberDto(user, user.Visibility.IsVisible())));
        }

        [HttpGet("admin")]
        [Authorize(Policy = PermissionPolicies.RolesEdit)]
        public async Task<IActionResult> GetAdminMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.Navn)
                .ToListAsync();

            var activeSubscriptionUserIds = await _db.PaymentSubscriptions
                .AsNoTracking()
                .Where(subscription => subscription.Status == PaymentSubscriptionStatuses.Active ||
                    subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                    subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                    subscription.Status == PaymentSubscriptionStatuses.Incomplete)
                .Select(subscription => subscription.UserId)
                .ToHashSetAsync();

            return Ok(members.Select(user => ToMemberDto(user, true, activeSubscriptionUserIds.Contains(user.Id))));
        }

        [HttpGet("{id}/subscriptions")]
        [Authorize(Policy = PermissionPolicies.RolesEdit)]
        public async Task<IActionResult> GetUserSubscriptions(string id)
        {
            var member = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.Id == id)
                .Select(user => new { user.Navn, user.Email })
                .SingleOrDefaultAsync();
            if (member == null)
            {
                return NotFound();
            }

            var subscriptions = await _db.PaymentSubscriptions
                .AsNoTracking()
                .Where(subscription => subscription.UserId == id &&
                    (subscription.Status == PaymentSubscriptionStatuses.Active ||
                        subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                        subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                        subscription.Status == PaymentSubscriptionStatuses.Incomplete))
                .OrderByDescending(subscription => subscription.Created)
                .Select(subscription => new AdminSubscriptionDto
                {
                    ProviderSubscriptionId = subscription.ProviderSubscriptionId,
                    Provider = subscription.Provider,
                    ProductName = subscription.ProductName,
                    ProviderProductId = subscription.ProviderProductId,
                    ProviderPriceId = subscription.ProviderPriceId,
                    Status = subscription.Status,
                    CurrentPeriodStart = subscription.CurrentPeriodStart,
                    CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                    NextChargeAt = subscription.NextChargeAt,
                    LastPaidAt = subscription.LastPaidAt,
                    CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    Created = subscription.Created,
                    Updated = subscription.Updated
                })
                .ToListAsync();

            foreach (var subscription in subscriptions)
            {
                subscription.ProviderDashboardUrl = BuildProviderDashboardUrl(subscription.Provider, subscription.ProviderSubscriptionId);
            }

            return Ok(new AdminMemberSubscriptionsDto
            {
                MemberName = member.Navn,
                MemberEmail = member.Email,
                Subscriptions = subscriptions
            });
        }

        [HttpGet("finance")]
        [Authorize(Policy = PermissionPolicies.FinanceMemberData)]
        public async Task<IActionResult> GetFinanceMembers()
        {
            var members = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.Navn)
                .Select(user => new FinanceMemberDto
                {
                    Id = user.Id,
                    Name = user.Navn
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpGet("event/{contentItemId:int}")]
        public async Task<IActionResult> GetEventMembers(int contentItemId)
        {
            if (!await _accessControl.CanEditRegistrationsAsync(User, contentItemId))
            {
                return Forbid();
            }

            var members = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.Navn)
                .ToListAsync();

            return Ok(members.Select(user => ToMemberDto(user, true)));
        }

        [HttpPut("{id}/status")]
        [Authorize(Policy = PermissionPolicies.RolesEdit)]
        public async Task<IActionResult> UpdateStatus(string id, UpdateMemberStatusRequest request)
        {
            if (!Enum.TryParse<UserStatus>(request?.Status, true, out var status))
            {
                return BadRequest(new { error = "Status er ugyldig" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = status;
            if (status == UserStatus.BETALT || status == UserStatus.STUDERENDE)
            {
                user.KontingentDato = DateTime.UtcNow;
            }

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? Ok(ToMemberDto(user, true)) : BadRequest(new { error = string.Join(", ", result.Errors.Select(error => error.Description)) });
        }

        [HttpPost("admin/mass-status")]
        [Authorize(Policy = PermissionPolicies.RolesEdit)]
        public async Task<IActionResult> UpdateMassStatus(MassUpdateMemberStatusRequest request)
        {
            if (!Enum.TryParse<UserStatus>(request?.Status, true, out var status))
            {
                return BadRequest(new { error = "Status er ugyldig" });
            }

            var users = await _userManager.Users
                .Where(user => user.KontingentDato >= request.From && user.KontingentDato <= request.To)
                .Where(user => !request.ExcludeActiveSubscriptions || !_db.PaymentSubscriptions.Any(subscription =>
                    subscription.UserId == user.Id && (subscription.Status == PaymentSubscriptionStatuses.Active ||
                        subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                        subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                        subscription.Status == PaymentSubscriptionStatuses.Incomplete)))
                .ToListAsync();

            var updated = 0;
            foreach (var user in users.Where(user => user.Status != status))
            {
                user.Status = status;
                if (status == UserStatus.BETALT || status == UserStatus.STUDERENDE)
                {
                    user.KontingentDato = DateTime.UtcNow;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    updated++;
                }
            }

            return Ok(new MassUpdateMemberStatusResult { Updated = updated });
        }

        private static MemberDto ToMemberDto(SiteUser user, bool includePrivate, bool? hasActiveSubscription = null)
        {
            return new MemberDto
            {
                Id = user.Id,
                Name = user.Navn,
                GraduationYear = user.Aargang,
                Occupation = includePrivate ? user.Beskaeftigelse : null,
                Email = includePrivate ? user.Email : null,
                PhoneNumber = includePrivate ? user.PhoneNumber : null,
                Status = user.Status.ToString(),
                IsVisible = user.Visibility.IsVisible(),
                MembershipPaidAt = user.KontingentDato,
                CreatedAt = user.OprettetDato,
                HasActiveSubscription = hasActiveSubscription
            };
        }

        private string BuildProviderDashboardUrl(string provider, string providerSubscriptionId)
        {
            if (!string.Equals(provider, "Stripe", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(providerSubscriptionId))
            {
                return null;
            }

            var accountId = _configuration?["StripeConfig:AccountId"]?.Trim();
            var environment = _configuration?["StripeConfig:Environment"]?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(accountId) || (environment != "test" && environment != "live"))
            {
                return null;
            }

            return $"https://dashboard.stripe.com/{Uri.EscapeDataString(accountId)}/{environment}/subscriptions/{Uri.EscapeDataString(providerSubscriptionId)}";
        }
    }
}
