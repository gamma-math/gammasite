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

        public ApiPaymentSubscriptionsController(
            ApplicationDbContext db,
            IStripeService stripeService = null,
            IMobilePayService mobilePayService = null)
        {
            _db = db;
            _stripeService = stripeService;
            _mobilePayService = mobilePayService;
        }

        [HttpGet]
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
            foreach (var subscription in subscriptions)
            {
                changed |= await RefreshMobilePaySubscriptionAsync(subscription, cancellationToken);
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
                    nextChargeAt = subscription.NextChargeAt,
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
            }
            else if (string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, System.StringComparison.OrdinalIgnoreCase))
            {
                if (_mobilePayService == null)
                {
                    return Problem("MobilePay-tjenesten er ikke konfigureret.");
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
            }
            else
            {
                return BadRequest("Dette abonnement kan ikke annulleres.");
            }

            subscription.Updated = System.DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                status = subscription.Status,
                canceled = true
            });
        }

        private async Task<bool> RefreshMobilePaySubscriptionAsync(
            PaymentSubscription subscription,
            CancellationToken cancellationToken)
        {
            if (_mobilePayService == null ||
                !string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) ||
                !IsOpenStatus(subscription.Status) ||
                subscription.ProviderSubscriptionId?.StartsWith("pending:", StringComparison.Ordinal) == true)
            {
                return false;
            }

            MobilePayAgreement agreement;
            try
            {
                agreement = await _mobilePayService.GetAgreementAsync(
                    subscription.ProviderSubscriptionId,
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            var providerStatus = agreement.Status?.Trim().ToUpperInvariant();
            var status = providerStatus switch
            {
                "ACTIVE" => PaymentSubscriptionStatuses.Active,
                "STOPPED" => PaymentSubscriptionStatuses.Canceled,
                "EXPIRED" or "REJECTED" => PaymentSubscriptionStatuses.Expired,
                _ => null
            };

            var changed = false;
            if (status != null && !string.Equals(subscription.Status, status, StringComparison.OrdinalIgnoreCase))
            {
                subscription.Status = status;
                changed = true;
            }

            if (status == PaymentSubscriptionStatuses.Active)
            {
                var periodStart = subscription.CurrentPeriodStart ?? agreement.StartAt ?? agreement.CreatedAt;
                if (periodStart.HasValue)
                {
                    if (!subscription.CurrentPeriodStart.HasValue)
                    {
                        subscription.CurrentPeriodStart = periodStart;
                        changed = true;
                    }

                    if (!subscription.LastPaidAt.HasValue)
                    {
                        subscription.LastPaidAt = periodStart;
                        changed = true;
                    }

                    if (!subscription.NextChargeAt.HasValue)
                    {
                        PaymentOffer offer = null;
                        if (int.TryParse(subscription.ProviderProductId, out var offerId))
                        {
                            offer = await _db.PaymentOffers
                                .FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken);
                        }
                        if (offer != null)
                        {
                            subscription.NextChargeAt = GetNextChargeDate(periodStart.Value, offer);
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                subscription.Updated = DateTime.UtcNow;
            }

            return changed;
        }

        private static DateTime GetNextChargeDate(DateTime paidAt, PaymentOffer offer)
        {
            var interval = string.IsNullOrWhiteSpace(offer.BillingInterval)
                ? "YEAR"
                : offer.BillingInterval.Trim().ToUpperInvariant();
            var count = offer.BillingIntervalCount.GetValueOrDefault(1);
            return interval switch
            {
                "DAY" => paidAt.AddDays(count),
                "WEEK" => paidAt.AddDays(count * 7),
                "MONTH" => paidAt.AddMonths(count),
                _ => paidAt.AddYears(count)
            };
        }

        private static bool IsOpenStatus(string status)
        {
            return status == PaymentSubscriptionStatuses.Pending ||
                status == PaymentSubscriptionStatuses.Active ||
                status == PaymentSubscriptionStatuses.Trialing ||
                status == PaymentSubscriptionStatuses.PastDue ||
                status == PaymentSubscriptionStatuses.Incomplete;
        }
    }
}
