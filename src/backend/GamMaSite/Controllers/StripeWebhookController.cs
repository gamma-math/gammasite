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
        private readonly ISystemEmailTemplateService _systemEmailTemplateService;

        public StripeWebhookController(
            ApplicationDbContext db,
            IStripeService stripeService,
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger,
            ISystemEmailTemplateService systemEmailTemplateService = null)
        {
            _db = db;
            _stripeService = stripeService;
            _configuration = configuration;
            _logger = logger;
            _systemEmailTemplateService = systemEmailTemplateService;
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

            var webhookId = stripeEvent.Id;
            var webhookAt = stripeEvent.Created.ToUniversalTime();
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                    await HandleCheckoutCompletedAsync(stripeEvent.Data.Object as Session, webhookId, webhookAt, cancellationToken);
                    break;
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionAsync(stripeEvent.Data.Object as Subscription, null, cancellationToken, webhookId, webhookAt);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionAsync(stripeEvent.Data.Object as Subscription, null, cancellationToken, webhookId, webhookAt);
                    break;
                case "invoice.paid":
                    await HandleInvoicePaidAsync(stripeEvent.Data.Object as Invoice, cancellationToken, webhookId, webhookAt);
                    break;
                case "invoice.payment_failed":
                case "invoice.payment_action_required":
                    await HandleInvoicePaymentFailedAsync(stripeEvent.Data.Object as Invoice, cancellationToken, webhookId, webhookAt);
                    break;
            }

            return Ok();
        }

        private async Task HandleCheckoutCompletedAsync(
            Session session,
            string webhookId,
            DateTime webhookAt,
            CancellationToken cancellationToken)
        {
            if (session == null)
            {
                return;
            }

            if (int.TryParse(MetadataValue(session.Metadata, "PaymentOfferId"), out var paymentOfferId))
            {
                await HandlePaymentOfferAsync(session, paymentOfferId, webhookId, webhookAt, cancellationToken);
                return;
            }

            var legacyEventOffer = await FindEventOfferAsync(session.Metadata, cancellationToken);
            if (legacyEventOffer != null)
            {
                await HandleEventPaymentAsync(session, legacyEventOffer.Id, cancellationToken);
                return;
            }

            var membershipOffer = await FindMembershipOfferAsync(session.Metadata, cancellationToken);
            if (membershipOffer == null)
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

                await HandleOneTimeMembershipPaymentAsync(
                    userId,
                    membershipOffer,
                    session.Created.ToUniversalTime(),
                    cancellationToken);
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
                cancellationToken,
                webhookId,
                webhookAt);
        }

        private async Task HandlePaymentOfferAsync(
            Session session,
            int paymentOfferId,
            string webhookId,
            DateTime webhookAt,
            CancellationToken cancellationToken)
        {
            var offer = await _db.PaymentOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == paymentOfferId, cancellationToken);

            if (offer == null)
            {
                // The offer may have been deleted after checkout was created.
                // The signed Stripe session still contains the event/user mapping.
                if (string.Equals(
                        MetadataValue(session.Metadata, "Scope"),
                        PaymentOfferScopes.Membership,
                        StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackOffer = new PaymentOffer
                    {
                        Scope = PaymentOfferScopes.Membership,
                        Provider = PaymentOfferProviders.Stripe,
                        OfferType = string.Equals(session.Mode, "subscription", StringComparison.OrdinalIgnoreCase)
                            ? PaymentOfferTypes.Recurring
                            : PaymentOfferTypes.OneTime,
                        ProductName = MetadataValue(session.Metadata, "ProductName") ?? "Medlemskab",
                        AmountMinor = session.AmountTotal,
                        Currency = session.Currency ?? "dkk"
                    };
                    await HandleMembershipOfferPaymentAsync(
                        session,
                        fallbackOffer,
                        webhookId,
                        webhookAt,
                        cancellationToken);
                    return;
                }

                if (int.TryParse(MetadataValue(session.Metadata, "ContentItemId"), out _))
                {
                    await HandleEventPaymentAsync(session, paymentOfferId, cancellationToken);
                    return;
                }

                _logger.LogWarning("Stripe payment session {SessionId} references unknown payment offer {PaymentOfferId}.", session.Id, paymentOfferId);
                return;
            }

            if (string.Equals(offer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase))
            {
                await HandleMembershipOfferPaymentAsync(session, offer, webhookId, webhookAt, cancellationToken);
                return;
            }

            if (string.Equals(offer.Scope, PaymentOfferScopes.Event, StringComparison.OrdinalIgnoreCase) ||
                offer.ContentItemId.HasValue)
            {
                await HandleEventPaymentAsync(session, paymentOfferId, cancellationToken);
            }
        }

        private async Task HandleMembershipOfferPaymentAsync(
            Session session,
            PaymentOffer offer,
            string webhookId,
            DateTime webhookAt,
            CancellationToken cancellationToken)
        {
            var userId = session.ClientReferenceId ?? MetadataValue(session.Metadata, "GamMaUserId");
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Stripe membership payment {SessionId} could not be mapped to a GamMa user.", session.Id);
                return;
            }

            var paymentConfirmed = string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(session.PaymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(session.Mode, "payment", StringComparison.OrdinalIgnoreCase))
            {
                if (paymentConfirmed)
                {
                    await HandleOneTimeMembershipPaymentAsync(
                        userId,
                        offer,
                        session.Created.ToUniversalTime(),
                        cancellationToken);
                }

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
                _logger.LogWarning("Stripe subscription {SubscriptionId} could not be loaded for membership checkout session {SessionId}.", session.SubscriptionId, session.Id);
                return;
            }

            await HandleSubscriptionAsync(
                subscription,
                new SubscriptionContext(
                    userId,
                    paymentConfirmed ? session.Created.ToUniversalTime() : null,
                    paymentConfirmed),
                cancellationToken,
                webhookId,
                webhookAt);
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

        private async Task<PaymentOffer> FindEventOfferAsync(
            System.Collections.Generic.IDictionary<string, string> metadata,
            CancellationToken cancellationToken)
        {
            var providerProductId = MetadataValue(metadata, "Product");
            var productName = MetadataValue(metadata, "ProductName");
            if (string.IsNullOrWhiteSpace(providerProductId))
            {
                return null;
            }

            return await _db.PaymentOffers
                .Where(item => item.Scope == PaymentOfferScopes.Event &&
                    item.Provider == PaymentOfferProviders.Stripe &&
                    item.ProviderProductId == providerProductId &&
                    (string.IsNullOrWhiteSpace(productName) || item.ProductName == productName))
                .OrderByDescending(item => item.Updated)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task HandleEventPaymentAsync(Session session, int paymentOfferId, CancellationToken cancellationToken)
        {
            if (!string.Equals(session.Mode, "payment", StringComparison.OrdinalIgnoreCase) ||
                (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(session.PaymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var userId = session.ClientReferenceId ?? MetadataValue(session.Metadata, "GamMaUserId");
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Event payment {SessionId} could not be mapped to a GamMa user.", session.Id);
                return;
            }

            var offer = await _db.PaymentOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == paymentOfferId, cancellationToken);

            var contentItemId = offer?.ContentItemId;
            if (!contentItemId.HasValue &&
                int.TryParse(MetadataValue(session.Metadata, "ContentItemId"), out var metadataContentItemId))
            {
                contentItemId = metadataContentItemId;
            }

            if (!contentItemId.HasValue)
            {
                _logger.LogWarning("Event payment {SessionId} references unknown payment offer {PaymentOfferId}.", session.Id, paymentOfferId);
                return;
            }

            var eventItem = await _db.ContentItems
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == contentItemId.Value && item.Type == ContentTypes.Event, cancellationToken);
            if (eventItem == null)
            {
                _logger.LogWarning("Event payment {SessionId} references unknown event {ContentItemId}.", session.Id, contentItemId);
                return;
            }

            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId.Value && item.UserId == userId, cancellationToken);
            if (registration == null ||
                (!string.Equals(registration.RegistrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(registration.RegistrationType, RegistrationTypes.Organizer, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Event payment {SessionId} has no attendee or organizer registration for user {UserId}.", session.Id, userId);
                return;
            }

            await using var transaction = await PaymentWebhookTransaction.BeginAsync(_db, cancellationToken);
            _db.Entry(registration).State = EntityState.Detached;
            registration = await PaymentWebhookTransaction.LockRegistrationAsync(_db, registration.Id, cancellationToken);
            if (registration == null ||
                (!string.Equals(registration.RegistrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(registration.RegistrationType, RegistrationTypes.Organizer, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (registration.Registered)
            {
                return;
            }

            registration.Registered = true;
            registration.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            if (offer != null)
            {
                await SendPaymentEmailAsync(
                    userId,
                    offer,
                    session.Created.ToUniversalTime(),
                    recurring: false,
                    cancellationToken: cancellationToken);
            }
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }

        private async Task HandleOneTimeMembershipPaymentAsync(
            string userId,
            PaymentOffer offer,
            DateTime paidAt,
            CancellationToken cancellationToken)
        {
            await using var transaction = await PaymentWebhookTransaction.BeginAsync(_db, cancellationToken);
            var member = await PaymentWebhookTransaction.LockUserAsync(_db, userId, cancellationToken);
            if (member == null)
            {
                _logger.LogWarning("Stripe payment could not be mapped to GamMa user {UserId}.", userId);
                return;
            }

            var wasAlreadyPaid = member.Status == UserStatus.BETALT;
            member.MarkAsPayed();
            if (member.KontingentDato == default || member.KontingentDato < paidAt)
            {
                member.KontingentDato = paidAt;
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (!wasAlreadyPaid)
            {
                await SendPaymentEmailAsync(
                    userId,
                    offer,
                    paidAt,
                    recurring: false,
                    cancellationToken: cancellationToken);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }

        private async Task HandleSubscriptionAsync(
            Subscription subscription,
            SubscriptionContext context,
            CancellationToken cancellationToken,
            string webhookId = null,
            DateTime? webhookAt = null)
        {
            if (subscription == null || string.IsNullOrWhiteSpace(subscription.Id))
            {
                return;
            }

            PaymentOffer paymentOffer = null;
            if (int.TryParse(MetadataValue(subscription.Metadata, "PaymentOfferId"), out var paymentOfferId))
            {
                paymentOffer = await _db.PaymentOffers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == paymentOfferId, cancellationToken);
            }

            var membershipOffer = paymentOffer == null
                ? await FindMembershipOfferAsync(subscription.Metadata, cancellationToken)
                : null;
            if (paymentOffer == null &&
                string.Equals(
                    MetadataValue(subscription.Metadata, "Scope"),
                    PaymentOfferScopes.Membership,
                    StringComparison.OrdinalIgnoreCase))
            {
                paymentOffer = new PaymentOffer
                {
                    Scope = PaymentOfferScopes.Membership,
                    Provider = PaymentOfferProviders.Stripe,
                    OfferType = PaymentOfferTypes.Recurring,
                    ProductName = MetadataValue(subscription.Metadata, "ProductName") ?? "Medlemskab"
                };
            }
            var isMembershipOffer = paymentOffer != null
                ? string.Equals(paymentOffer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(paymentOffer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase)
                : membershipOffer != null;
            if (!isMembershipOffer)
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

            await using var transaction = await PaymentWebhookTransaction.BeginAsync(_db, cancellationToken);
            if (record != null)
            {
                var recordId = record.Id;
                _db.Entry(record).State = EntityState.Detached;
                record = await PaymentWebhookTransaction.LockSubscriptionAsync(_db, recordId, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(webhookId) &&
                string.Equals(record?.LastProcessedWebhookId, webhookId, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Ignoring already processed Stripe webhook {WebhookId} for subscription {SubscriptionId}.",
                    webhookId,
                    subscription.Id);
                return;
            }

            // Different provider events can share the same second. Only an
            // strictly older event is out of order; the row lock above makes
            // the check and state write atomic for concurrent deliveries.
            if (record?.LastProcessedWebhookAt.HasValue == true &&
                webhookAt.HasValue &&
                webhookAt.Value < record.LastProcessedWebhookAt.Value)
            {
                _logger.LogInformation(
                    "Ignoring out-of-order Stripe webhook {WebhookId} for subscription {SubscriptionId}.",
                    webhookId,
                    subscription.Id);
                return;
            }

            var paymentDate = context?.LastPaidAt ?? DateTime.UtcNow;
            var shouldSendPaymentEmail = context?.MarkUserAsPaid == true &&
                (!record?.LastPaidAt.HasValue ?? true || record.LastPaidAt.Value < paymentDate);

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

            ApplySubscriptionState(record);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsPaymentSubscriptionDuplicate(exception) &&
                _db.Entry(record).State == EntityState.Added)
            {
                // Stripe can deliver checkout.session.completed and subscription events
                // concurrently. The unique user index is the final safeguard; if the
                // competing request won the insert, reuse its current-state row.
                _db.Entry(record).State = EntityState.Detached;
                record = await _db.PaymentSubscriptions
                    .SingleOrDefaultAsync(item => item.Provider == Provider && item.ProviderSubscriptionId == subscription.Id, cancellationToken);

                if (record == null && !string.IsNullOrWhiteSpace(userId))
                {
                    record = await _db.PaymentSubscriptions
                        .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
                }

                if (record == null)
                {
                    throw;
                }

                var recordId = record.Id;
                _db.Entry(record).State = EntityState.Detached;
                record = await PaymentWebhookTransaction.LockSubscriptionAsync(_db, recordId, cancellationToken);
                if (record == null)
                {
                    throw;
                }

                if (!string.IsNullOrWhiteSpace(webhookId) &&
                    string.Equals(record.LastProcessedWebhookId, webhookId, StringComparison.Ordinal))
                {
                    return;
                }

                if (record.LastProcessedWebhookAt.HasValue &&
                    webhookAt.HasValue &&
                    webhookAt.Value < record.LastProcessedWebhookAt.Value)
                {
                    return;
                }

                shouldSendPaymentEmail = context?.MarkUserAsPaid == true &&
                    (!record.LastPaidAt.HasValue || record.LastPaidAt.Value < paymentDate);
                ApplySubscriptionState(record);
                await _db.SaveChangesAsync(cancellationToken);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            if (context?.MarkUserAsPaid == true && !string.IsNullOrWhiteSpace(record.UserId))
            {
                await MarkMembershipAsPaidAsync(
                    record.UserId,
                    paymentDate,
                    cancellationToken);

                if (shouldSendPaymentEmail)
                {
                    await SendPaymentEmailAsync(
                        record.UserId,
                        paymentOffer ?? membershipOffer,
                        paymentDate,
                        recurring: true,
                        cancellationToken: cancellationToken);
                }
            }

            void ApplySubscriptionState(PaymentSubscription target)
            {
                target.UserId = userId ?? target.UserId;
                target.PaymentOfferId = paymentOffer?.Id ?? membershipOffer?.Id ?? target.PaymentOfferId;
                target.Provider = Provider;
                target.ProviderSubscriptionId = subscription.Id;
                target.ProductName = productName ?? target.ProductName;
                target.ProviderProductId = productId ?? target.ProviderProductId;
                target.ProviderPriceId = price?.Id ?? target.ProviderPriceId;
                target.Status = subscription.Status;
                target.CurrentPeriodStart = subscriptionItem?.CurrentPeriodStart;
                target.CurrentPeriodEnd = subscriptionItem?.CurrentPeriodEnd;
                target.NextChargeAt = string.Equals(subscription.Status, "canceled", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : subscriptionItem?.CurrentPeriodEnd;
                target.LastPaidAt = context?.LastPaidAt ?? target.LastPaidAt;
                target.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;
                if (!string.IsNullOrWhiteSpace(webhookId))
                {
                    target.LastProcessedWebhookId = webhookId;
                    target.LastProcessedWebhookAt = webhookAt ?? DateTime.UtcNow;
                }
                target.Updated = DateTime.UtcNow;
            }
        }

        private static bool IsPaymentSubscriptionDuplicate(DbUpdateException exception)
        {
            var message = exception.ToString();
            return message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) &&
                (message.Contains("UX_PaymentSubscriptions_UserId", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("UX_PaymentSubscriptions_ProviderSubscriptionId", StringComparison.OrdinalIgnoreCase));
        }

        private async Task HandleInvoicePaidAsync(
            Invoice invoice,
            CancellationToken cancellationToken,
            string webhookId,
            DateTime webhookAt)
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
                    cancellationToken,
                    webhookId,
                    webhookAt);
            }
        }

        private async Task HandleInvoicePaymentFailedAsync(
            Invoice invoice,
            CancellationToken cancellationToken,
            string webhookId,
            DateTime webhookAt)
        {
            var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return;
            }

            var subscription = await _stripeService.GetSubscriptionAsync(subscriptionId);
            if (subscription != null)
            {
                await HandleSubscriptionAsync(subscription, null, cancellationToken, webhookId, webhookAt);
                return;
            }

            var record = await _db.PaymentSubscriptions
                .SingleOrDefaultAsync(item => item.Provider == Provider && item.ProviderSubscriptionId == subscriptionId, cancellationToken);
            if (record == null)
            {
                return;
            }

            await using var transaction = await PaymentWebhookTransaction.BeginAsync(_db, cancellationToken);
            var recordId = record.Id;
            _db.Entry(record).State = EntityState.Detached;
            record = await PaymentWebhookTransaction.LockSubscriptionAsync(_db, recordId, cancellationToken);
            if (record == null ||
                string.Equals(record.LastProcessedWebhookId, webhookId, StringComparison.Ordinal) ||
                (record.LastProcessedWebhookAt.HasValue &&
                    webhookAt < record.LastProcessedWebhookAt.Value))
            {
                return;
            }

            record.Status = "past_due";
            record.LastProcessedWebhookId = webhookId;
            record.LastProcessedWebhookAt = webhookAt;
            record.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }

        private async Task<PaymentOffer> FindMembershipOfferAsync(
            System.Collections.Generic.IDictionary<string, string> metadata,
            CancellationToken cancellationToken)
        {
            var productId = MetadataValue(metadata, "Product");
            if (string.IsNullOrWhiteSpace(productId))
            {
                return null;
            }

            return await _db.PaymentOffers
                .Where(item => item.Scope == PaymentOfferScopes.Membership &&
                    item.Provider == PaymentOfferProviders.Stripe &&
                    item.ProviderProductId == productId)
                .OrderByDescending(item => item.Updated)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task SendPaymentEmailAsync(
            string userId,
            PaymentOffer offer,
            DateTime paymentDate,
            bool recurring,
            CancellationToken cancellationToken)
        {
            if (_systemEmailTemplateService == null || offer == null || string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var user = await _db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Stripe payment for user {UserId} could not be emailed because the user or email is missing.", userId);
                return;
            }

            await _systemEmailTemplateService.SendPaymentConfirmationAsync(
                user.Email,
                user.Navn,
                offer.ProductName,
                offer.AmountMinor.GetValueOrDefault(),
                offer.Currency,
                PaymentOfferProviders.Stripe,
                paymentDate,
                recurring,
                offer.BillingInterval,
                offer.BillingIntervalCount,
                null);
        }

        private static string MetadataValue(System.Collections.Generic.IDictionary<string, string> metadata, string key)
        {
            return metadata != null && metadata.TryGetValue(key, out var value) ? value : null;
        }

        private sealed record SubscriptionContext(string UserId, DateTime? LastPaidAt, bool MarkUserAsPaid = false);
    }
}
