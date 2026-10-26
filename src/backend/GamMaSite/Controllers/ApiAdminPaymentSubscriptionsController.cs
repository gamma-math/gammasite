using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
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
        private readonly IStripeService _stripeService;

        public ApiAdminPaymentSubscriptionsController(
            ApplicationDbContext db,
            IMobilePayService mobilePayService = null,
            IStripeService stripeService = null)
        {
            _db = db;
            _mobilePayService = mobilePayService;
            _stripeService = stripeService;
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
                return ToDto(subscription, user);
            }));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var subscription = await _db.PaymentSubscriptions
                .Include(item => item.PaymentOffer)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (subscription == null)
            {
                return NotFound();
            }

            var user = await _db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == subscription.UserId, cancellationToken);

            var payments = Array.Empty<AdminSubscriptionPaymentDto>();
            string paymentsError = null;

            try
            {
                if (string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                    _mobilePayService != null &&
                    HasProviderSubscriptionId(subscription))
                {
                    var charges = await _mobilePayService.GetRecurringChargesAsync(
                        subscription.ProviderSubscriptionId,
                        cancellationToken);

                    payments = charges
                        .OrderByDescending(charge => charge.Due ?? DateTime.MinValue)
                        .Select(charge => new AdminSubscriptionPaymentDto
                        {
                            Id = charge.Id,
                            Type = charge.Type,
                            Status = charge.Status,
                            AmountMinor = charge.AmountMinor,
                            Currency = charge.Currency,
                            Description = charge.Description,
                            DueAt = charge.Due,
                            TransactionId = charge.TransactionId,
                            FailureReason = charge.FailureReason
                        })
                        .ToArray();
                }
                else if (string.Equals(subscription.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase) &&
                    _stripeService != null &&
                    HasProviderSubscriptionId(subscription))
                {
                    var invoices = await _stripeService.GetSubscriptionPaymentsAsync(
                        subscription.ProviderSubscriptionId);

                    payments = invoices
                        .Select(invoice => new AdminSubscriptionPaymentDto
                        {
                            Id = invoice.Id,
                            Type = "INVOICE",
                            Status = invoice.Status,
                            AmountMinor = invoice.AmountMinor,
                            Currency = invoice.Currency,
                            Description = invoice.Description,
                            DueAt = invoice.DueAt,
                            CreatedAt = invoice.CreatedAt,
                            PaidAt = invoice.PaidAt
                        })
                        .ToArray();
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException ||
                exception is HttpRequestException ||
                exception is TaskCanceledException ||
                exception is Stripe.StripeException)
            {
                paymentsError = "Betalingerne kunne ikke hentes hos betalingsudbyderen.";
            }

            return Ok(new AdminPaymentSubscriptionDetailDto
            {
                Subscription = ToDto(subscription, user),
                Payments = payments,
                PaymentsError = paymentsError
            });
        }

        private static AdminPaymentSubscriptionDto ToDto(PaymentSubscription subscription, SiteUser user)
        {
            var offer = subscription.PaymentOffer;
            return new AdminPaymentSubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                UserName = user?.Navn,
                UserEmail = user?.Email,
                Provider = subscription.Provider,
                ProviderSubscriptionId = subscription.ProviderSubscriptionId,
                PaymentOfferId = subscription.PaymentOfferId,
                ProviderProductId = subscription.ProviderProductId,
                ProviderPriceId = subscription.ProviderPriceId,
                ProductName = subscription.ProductName ?? offer?.ProductName,
                AmountMinor = offer?.AmountMinor,
                Currency = offer?.Currency,
                OfferType = offer?.OfferType,
                Scope = offer?.Scope,
                Status = subscription.Status,
                CurrentPeriodStart = subscription.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                LastPaidAt = subscription.LastPaidAt,
                NextChargeAt = IsTerminalStatus(subscription.Status) ? null : subscription.NextChargeAt,
                CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                Created = subscription.Created,
                Updated = subscription.Updated,
                LastProcessedWebhookId = subscription.LastProcessedWebhookId,
                LastProcessedWebhookAt = subscription.LastProcessedWebhookAt
            };
        }

        private static bool HasProviderSubscriptionId(PaymentSubscription subscription)
        {
            return !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId) &&
                !subscription.ProviderSubscriptionId.StartsWith("pending:", StringComparison.Ordinal);
        }

        private static bool IsTerminalStatus(string status)
        {
            return string.Equals(status, PaymentSubscriptionStatuses.Canceled, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, PaymentSubscriptionStatuses.Expired, StringComparison.OrdinalIgnoreCase);
        }
    }
}
