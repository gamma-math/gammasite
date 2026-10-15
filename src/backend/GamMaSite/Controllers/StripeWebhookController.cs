using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace GamMaSite.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/payments/stripe")]
    public sealed class StripeWebhookController : ControllerBase
    {
        private const string Provider = "Stripe";

        private readonly ApplicationDbContext _db;
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            ApplicationDbContext db,
            IStripeService stripeService,
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger)
        {
            _db = db;
            _stripeService = stripeService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Receive(CancellationToken cancellationToken)
        {
            var endpointSecret = _configuration["StripeConfig:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(endpointSecret))
            {
                _logger.LogError("Stripe webhook secret is not configured.");
                return Problem("Stripe webhook er ikke konfigureret.");
            }

            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature = Request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;
            try
            {
                // Signature verification remains enabled while accepting Stripe event API versions
                // that may differ from the SDK's default version.
                stripeEvent = EventUtility.ConstructEvent(payload, signature, endpointSecret, 300, false);
            }
            catch (Exception exception) when (exception is StripeException || exception is ArgumentException || exception is System.Text.Json.JsonException)
            {
                _logger.LogWarning(exception, "Stripe webhook signature or payload validation failed.");
                return BadRequest();
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                    await HandleCheckoutCompletedAsync(stripeEvent.Data.Object as Session, cancellationToken);
                    break;
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionAsync(stripeEvent.Data.Object as Subscription, null, cancellationToken);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionAsync(stripeEvent.Data.Object as Subscription, null, cancellationToken);
                    break;
                case "invoice.paid":
                    await HandleInvoicePaidAsync(stripeEvent.Data.Object as Invoice, cancellationToken);
                    break;
                case "invoice.payment_failed":
                case "invoice.payment_action_required":
                    await HandleInvoicePaymentFailedAsync(stripeEvent.Data.Object as Invoice, cancellationToken);
                    break;
            }

            return Ok();
        }

        private async Task HandleCheckoutCompletedAsync(Session session, CancellationToken cancellationToken)
        {
            if (session == null || !IsMembershipProduct(session.Metadata))
            {
                return;
            }

            var userId = session.ClientReferenceId ?? MetadataValue(session.Metadata, "GamMaUserId");
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Stripe checkout session {SessionId} could not be mapped to a GamMa user.", session.Id);
                return;
            }

            if (string.Equals(session.Mode, "payment", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(session.PaymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                await MarkMembershipAsPaidAsync(userId, session.Created.ToUniversalTime(), cancellationToken);
                return;
            }

            if (!string.Equals(session.Mode, "subscription", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(session.SubscriptionId))
            {
                return;
            }

            var subscription = await _stripeService.GetSubscriptionAsync(session.SubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("Stripe subscription {SubscriptionId} could not be loaded for checkout session {SessionId}.", session.SubscriptionId, session.Id);
                return;
            }

            var initialPaymentAt = string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
                ? session.Created.ToUniversalTime()
                : (DateTime?)null;

            var paymentConfirmed = string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(session.PaymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase);

            await HandleSubscriptionAsync(
                subscription,
                new SubscriptionContext(userId, initialPaymentAt, paymentConfirmed),
                cancellationToken);
        }

        private async Task MarkMembershipAsPaidAsync(string userId, DateTime paidAt, CancellationToken cancellationToken)
        {
            var user = await _db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Stripe payment could not be mapped to GamMa user {UserId}.", userId);
                return;
            }

            user.MarkAsPayed();
            if (user.KontingentDato == default || user.KontingentDato < paidAt)
            {
                user.KontingentDato = paidAt;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task HandleSubscriptionAsync(
            Subscription subscription,
            SubscriptionContext context,
            CancellationToken cancellationToken)
        {
            if (subscription == null || string.IsNullOrWhiteSpace(subscription.Id) ||
                !IsMembershipProduct(subscription.Metadata))
            {
                return;
            }

            var userId = context?.UserId ?? MetadataValue(subscription.Metadata, "GamMaUserId");
            var record = await _db.PaymentSubscriptions
                .SingleOrDefaultAsync(item => item.Provider == Provider && item.ProviderSubscriptionId == subscription.Id, cancellationToken);

            if (record == null && !string.IsNullOrWhiteSpace(userId))
            {
                record = await _db.PaymentSubscriptions
                    .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
            }

            if (record == null && string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Stripe subscription {SubscriptionId} could not be mapped to a GamMa user.", subscription.Id);
                return;
            }

            var subscriptionItem = subscription.Items?.Data?.FirstOrDefault();
            var price = subscriptionItem?.Price;
            var productId = price?.ProductId;
            var productName = MetadataValue(subscription.Metadata, "ProductName");
            if (!string.IsNullOrWhiteSpace(productId))
            {
                var product = await _stripeService.GetProductAsync(productId);
                productName = product?.Name ?? productName;
            }

            if (record == null)
            {
                record = new PaymentSubscription
                {
                    UserId = userId,
                    Provider = Provider,
                    ProviderSubscriptionId = subscription.Id,
                    Created = subscription.Created.ToUniversalTime()
                };
                _db.PaymentSubscriptions.Add(record);
            }

            record.UserId = userId ?? record.UserId;
            record.Provider = Provider;
            record.ProviderSubscriptionId = subscription.Id;
            record.ProductName = productName ?? record.ProductName;
            record.ProviderProductId = productId ?? record.ProviderProductId;
            record.ProviderPriceId = price?.Id ?? record.ProviderPriceId;
            record.Status = subscription.Status;
            record.CurrentPeriodStart = subscriptionItem?.CurrentPeriodStart;
            record.CurrentPeriodEnd = subscriptionItem?.CurrentPeriodEnd;
            record.NextChargeAt = subscriptionItem?.CurrentPeriodEnd;
            record.LastPaidAt = context?.LastPaidAt ?? record.LastPaidAt;
            record.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;
            record.Updated = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            if (context?.MarkUserAsPaid == true && !string.IsNullOrWhiteSpace(record.UserId))
            {
                await MarkMembershipAsPaidAsync(
                    record.UserId,
                    context.LastPaidAt ?? DateTime.UtcNow,
                    cancellationToken);
            }
        }

        private async Task HandleInvoicePaidAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return;
            }

            var subscription = await _stripeService.GetSubscriptionAsync(subscriptionId);
            if (subscription != null)
            {
                await HandleSubscriptionAsync(
                    subscription,
                    new SubscriptionContext(null, invoice.Created.ToUniversalTime(), true),
                    cancellationToken);
            }
        }

        private async Task HandleInvoicePaymentFailedAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return;
            }

            var subscription = await _stripeService.GetSubscriptionAsync(subscriptionId);
            if (subscription != null)
            {
                await HandleSubscriptionAsync(subscription, null, cancellationToken);
                return;
            }

            var record = await _db.PaymentSubscriptions
                .SingleOrDefaultAsync(item => item.Provider == Provider && item.ProviderSubscriptionId == subscriptionId, cancellationToken);
            if (record == null)
            {
                return;
            }

            record.Status = "past_due";
            record.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private bool IsMembershipProduct(System.Collections.Generic.IDictionary<string, string> metadata)
        {
            var configuredProductId = _configuration["StripeConfig:MembershipProductId"];
            var productId = MetadataValue(metadata, "Product");
            return !string.IsNullOrWhiteSpace(configuredProductId) &&
                string.Equals(configuredProductId, productId, StringComparison.Ordinal);
        }

        private static string MetadataValue(System.Collections.Generic.IDictionary<string, string> metadata, string key)
        {
            return metadata != null && metadata.TryGetValue(key, out var value) ? value : null;
        }

        private sealed record SubscriptionContext(string UserId, DateTime? LastPaidAt, bool MarkUserAsPaid = false);
    }
}
