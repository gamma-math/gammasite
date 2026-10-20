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
        private readonly IMobilePayService _mobilePayService;

        public ApiAdminPaymentSubscriptionsController(
            ApplicationDbContext db,
            IMobilePayService mobilePayService = null)
        {
            _db = db;
            _mobilePayService = mobilePayService;
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
                    productName = subscription.ProductName ?? offer?.ProductName,
                    amountMinor = offer?.AmountMinor,
                    currency = offer?.Currency,
                    offerType = offer?.OfferType,
                    scope = offer?.Scope,
                    status = subscription.Status,
                    currentPeriodStart = subscription.CurrentPeriodStart,
                    currentPeriodEnd = subscription.CurrentPeriodEnd,
                    lastPaidAt = subscription.LastPaidAt,
                    nextChargeAt = subscription.NextChargeAt,
                    cancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    created = subscription.Created,
                    updated = subscription.Updated,
                    canCharge = string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(offer?.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId) &&
                        subscription.Status == PaymentSubscriptionStatuses.Active
                };
            }));
        }

        [HttpPost("{id:int}/charge")]
        public async Task<IActionResult> Charge(int id, CancellationToken cancellationToken)
        {
            if (_mobilePayService == null)
            {
                return Problem("MobilePay-tjenesten er ikke konfigureret.");
            }

            var subscription = await _db.PaymentSubscriptions
                .Include(item => item.PaymentOffer)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (subscription == null)
            {
                return NotFound();
            }

            var offer = subscription.PaymentOffer;
            if (!string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(offer?.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Kun gentagne MobilePay-aftaler kan opkræves her.");
            }

            if (subscription.Status != PaymentSubscriptionStatuses.Active ||
                string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId) ||
                offer.AmountMinor.GetValueOrDefault() <= 0)
            {
                return BadRequest("Betalingsaftalen er ikke aktiv eller mangler et gyldigt beløb.");
            }

            var due = DateTime.UtcNow.Date.AddDays(1).AddHours(1);
            var orderId = $"gamma-admin-{subscription.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            try
            {
                await _mobilePayService.CreateRecurringChargeAsync(
                    subscription.ProviderSubscriptionId,
                    orderId,
                    offer.AmountMinor.Value,
                    offer.Currency ?? "DKK",
                    offer.ProductName,
                    due,
                    cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(exception.Message);
            }

            return Ok(new { orderId, due });
        }
    }
}
