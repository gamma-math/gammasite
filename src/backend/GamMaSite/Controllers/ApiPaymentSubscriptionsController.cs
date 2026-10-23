using System.Linq;
using System.Security.Claims;
using System;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    [Route("api/account/subscriptions")]
    public sealed class ApiPaymentSubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IStripeService _stripeService;
        private readonly IMobilePayService _mobilePayService;
        private readonly MobilePaySubscriptionReconciliationService _mobilePayReconciliation;
        private readonly ILogger<ApiPaymentSubscriptionsController> _logger;

        public ApiPaymentSubscriptionsController(
            ApplicationDbContext db,
            IStripeService stripeService = null,
            IMobilePayService mobilePayService = null,
            ILogger<ApiPaymentSubscriptionsController> logger = null,
            MobilePaySubscriptionReconciliationService mobilePayReconciliation = null)
        {
            _db = db;
            _stripeService = stripeService;
            _mobilePayService = mobilePayService;
            _logger = logger;
            _mobilePayReconciliation = mobilePayReconciliation ??
                new MobilePaySubscriptionReconciliationService(db, mobilePayService);
        }

        [HttpGet]
        [EnableRateLimiting("payment-provider-read")]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var subscriptions = await _db.PaymentSubscriptions
                .Include(subscription => subscription.PaymentOffer)
                .Where(subscription => subscription.UserId == userId)
                .ToListAsync(cancellationToken);

            var changed = false;
            for (var index = 0; index < subscriptions.Count; index++)
            {
                var subscription = subscriptions[index];
                if (IsTerminalStatus(subscription.Status) && subscription.NextChargeAt.HasValue)
                {
                    subscription.NextChargeAt = null;
                    subscription.Updated = DateTime.UtcNow;
                    changed = true;
                }

                subscriptions[index] = await _mobilePayReconciliation.ReconcileAsync(
                    subscription,
                    cancellationToken);
            }

            if (changed)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            var result = subscriptions
                .OrderByDescending(subscription => subscription.Created)
                .Select(subscription => new
                {
                    providerSubscriptionId = subscription.ProviderSubscriptionId,
                    provider = subscription.Provider,
                    paymentOfferId = subscription.PaymentOfferId,
                    productName = subscription.ProductName,
                    amountMinor = subscription.PaymentOffer == null ? null : subscription.PaymentOffer.AmountMinor,
                    currency = subscription.PaymentOffer == null ? null : subscription.PaymentOffer.Currency,
                    status = subscription.Status,
                    lastPaidAt = subscription.LastPaidAt,
                    nextChargeAt = IsTerminalStatus(subscription.Status) ? null : subscription.NextChargeAt,
                    created = subscription.Created,
                    updated = subscription.Updated
                });

            return Ok(result);
        }

        [HttpDelete("{providerSubscriptionId}")]
        public async Task<IActionResult> Delete(string providerSubscriptionId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var subscription = await _db.PaymentSubscriptions
                .SingleOrDefaultAsync(item => item.UserId == userId &&
                    item.ProviderSubscriptionId == providerSubscriptionId, cancellationToken);
            if (subscription == null)
            {
                return NotFound();
            }

            if (string.Equals(subscription.Provider, PaymentOfferProviders.Stripe, System.StringComparison.OrdinalIgnoreCase))
            {
                if (_stripeService == null)
                {
                    return Problem("Stripe-tjenesten er ikke konfigureret.");
                }

                var canceledSubscription = await _stripeService.CancelSubscriptionAsync(providerSubscriptionId);
                if (canceledSubscription == null)
                {
                    return BadRequest("Abonnementet kunne ikke annulleres hos betalingsudbyderen.");
                }

                subscription.Status = canceledSubscription.Status;
                subscription.CancelAtPeriodEnd = canceledSubscription.CancelAtPeriodEnd;
                if (string.Equals(subscription.Status, PaymentSubscriptionStatuses.Canceled, StringComparison.OrdinalIgnoreCase))
                {
                    subscription.NextChargeAt = null;
                }
            }
            else if (string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, System.StringComparison.OrdinalIgnoreCase))
            {
                if (_mobilePayService == null)
                {
                    return Problem("MobilePay-tjenesten er ikke konfigureret.");
                }

                if (string.Equals(subscription.Status, PaymentSubscriptionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                {
                    var wasAbandoned = await _mobilePayReconciliation.TryAbandonPendingAsync(
                        subscription,
                        cancellationToken);
                    if (wasAbandoned)
                    {
                        var updatedSubscription = await _db.PaymentSubscriptions
                            .AsNoTracking()
                            .SingleAsync(item => item.Id == subscription.Id, cancellationToken);
                        return Ok(new
                        {
                            status = updatedSubscription.Status,
                            canceled = true
                        });
                    }
                }

                try
                {
                    await _mobilePayService.StopAgreementAsync(providerSubscriptionId, cancellationToken);
                }
                catch (System.InvalidOperationException)
                {
                    return StatusCode(502, "Aftalen kunne ikke annulleres hos betalingsudbyderen.");
                }

                subscription.Status = PaymentSubscriptionStatuses.Canceled;
                subscription.CancelAtPeriodEnd = false;
                subscription.NextChargeAt = null;
            }
            else
            {
                return BadRequest("Dette abonnement kan ikke annulleres.");
            }

            subscription.Updated = System.DateTime.UtcNow;
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger?.LogError(
                    exception,
                    "Provider subscription {ProviderSubscriptionId} was canceled, but the local cancellation state could not be saved for user {UserId}.",
                    providerSubscriptionId,
                    userId);
                return StatusCode(502, "Aftalen blev annulleret hos betalingsudbyderen, men den lokale status kunne ikke gemmes.");
            }

            return Ok(new
            {
                status = subscription.Status,
                canceled = true
            });
        }

        private static bool IsTerminalStatus(string status)
        {
            return string.Equals(status, PaymentSubscriptionStatuses.Canceled, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, PaymentSubscriptionStatuses.Expired, StringComparison.OrdinalIgnoreCase);
        }
    }
}
