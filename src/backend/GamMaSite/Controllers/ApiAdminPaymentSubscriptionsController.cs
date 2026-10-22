using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Authorize(Policy = PermissionPolicies.PaymentsEditAll)]
    [AutoValidateAntiforgeryToken]
    [Route("api/payment-subscriptions/admin")]
    public sealed class ApiAdminPaymentSubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ApiAdminPaymentSubscriptionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var subscriptions = await _db.PaymentSubscriptions
                .Include(subscription => subscription.PaymentOffer)
                .AsNoTracking()
                .OrderByDescending(subscription => subscription.Created)
                .ToListAsync(cancellationToken);

            var userIds = subscriptions
                .Select(subscription => subscription.UserId)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Distinct()
                .ToList();
            var users = await _db.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, cancellationToken);

            return Ok(subscriptions.Select(subscription =>
            {
                users.TryGetValue(subscription.UserId, out var user);
                var offer = subscription.PaymentOffer;
                return new
                {
                    id = subscription.Id,
                    userId = subscription.UserId,
                    userName = user?.Navn,
                    userEmail = user?.Email,
                    provider = subscription.Provider,
                    providerSubscriptionId = subscription.ProviderSubscriptionId,
                    paymentOfferId = subscription.PaymentOfferId,
                    providerProductId = subscription.ProviderProductId,
                    providerPriceId = subscription.ProviderPriceId,
                    productName = subscription.ProductName ?? offer?.ProductName,
                    amountMinor = offer?.AmountMinor,
                    currency = offer?.Currency,
                    offerType = offer?.OfferType,
                    scope = offer?.Scope,
                    status = subscription.Status,
                    currentPeriodStart = subscription.CurrentPeriodStart,
                    currentPeriodEnd = subscription.CurrentPeriodEnd,
                    lastPaidAt = subscription.LastPaidAt,
                    nextChargeAt = IsTerminalStatus(subscription.Status) ? null : subscription.NextChargeAt,
                    cancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    created = subscription.Created,
                    updated = subscription.Updated,
                    lastProcessedWebhookId = subscription.LastProcessedWebhookId,
                    lastProcessedWebhookAt = subscription.LastProcessedWebhookAt
                };
            }));
        }

        private static bool IsTerminalStatus(string status)
        {
            return string.Equals(status, PaymentSubscriptionStatuses.Canceled, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, PaymentSubscriptionStatuses.Expired, StringComparison.OrdinalIgnoreCase);
        }
    }
}
