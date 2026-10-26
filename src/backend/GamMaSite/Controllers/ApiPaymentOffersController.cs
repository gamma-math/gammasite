using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GamMaSite.Controllers
{
    [ApiController]
    [Route("api/payment-offers")]
    [AutoValidateAntiforgeryToken]
    public sealed class ApiPaymentOffersController : ControllerBase
    {
        private const string Currency = "dkk";
        private const long MinimumAmountMinor = 1000;
        private readonly ApplicationDbContext _db;
        private readonly IStripeService _stripeService;
        private readonly IMobilePayService _mobilePayService;
        private readonly MobilePaySubscriptionReconciliationService _mobilePayReconciliation;
        private readonly IMobilePayEventReferenceService _mobilePayReferenceService;
        private readonly IAccessControlService _accessControl;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiPaymentOffersController> _logger;

        public ApiPaymentOffersController(
            ApplicationDbContext db,
            IStripeService stripeService,
            IAccessControlService accessControl,
            IMobilePayService mobilePayService = null,
            IMobilePayEventReferenceService mobilePayReferenceService = null,
            IConfiguration configuration = null,
            ILogger<ApiPaymentOffersController> logger = null,
            MobilePaySubscriptionReconciliationService mobilePayReconciliation = null)
        {
            _db = db;
            _stripeService = stripeService;
            _mobilePayService = mobilePayService;
            _mobilePayReconciliation = mobilePayReconciliation ??
                new MobilePaySubscriptionReconciliationService(db, mobilePayService);
            _mobilePayReferenceService = mobilePayReferenceService;
            _accessControl = accessControl;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("event/{contentItemId:int}")]
        [Authorize]
        public async Task<IActionResult> GetForEvent(int contentItemId)
        {
            if (!await _accessControl.CanEditEventAsync(User, contentItemId))
            {
                return Forbid();
            }

            var offers = await _db.PaymentOffers
                .Include(offer => offer.ContentItem)
                .ThenInclude(item => item.Links)
                .Where(offer => offer.ContentItemId == contentItemId)
                .OrderBy(offer => offer.Created)
                .ThenBy(offer => offer.Id)
                .ToListAsync();

            return Ok(offers.Select(offer => offer.ToDto()));
        }

        [HttpGet("admin")]
        [Authorize(Policy = PermissionPolicies.PaymentsEditAll)]
        public async Task<IActionResult> GetForAdmin()
        {
            var offers = await _db.PaymentOffers
                .Include(offer => offer.ContentItem)
                .AsNoTracking()
                .OrderBy(offer => offer.Scope)
                .ThenBy(offer => offer.ContentItem == null ? string.Empty : offer.ContentItem.Title)
                .ThenBy(offer => offer.ProductName)
                .ThenBy(offer => offer.Provider)
                .ThenBy(offer => offer.OfferType)
                .ThenBy(offer => offer.Id)
                .ToListAsync();

            return Ok(offers.Select(offer => offer.ToDto()));
        }

        [HttpGet("admin/events")]
        [Authorize(Policy = PermissionPolicies.PaymentsEditAll)]
        public async Task<IActionResult> GetEventOptionsForAdmin()
        {
            var events = await _db.ContentItems
                .AsNoTracking()
                .Where(item => item.Type == ContentTypes.Event)
                .OrderBy(item => item.Title)
                .Select(item => new
                {
                    id = item.Id,
                    title = item.Title
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("admin/{id:int}")]
        [Authorize(Policy = PermissionPolicies.PaymentsEditAll)]
        public async Task<IActionResult> GetForAdmin(int id)
        {
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);

            return offer == null ? NotFound() : Ok(offer.ToDto());
        }

        [HttpGet("membership")]
        [Authorize]
        public async Task<IActionResult> GetMembershipOffers()
        {
            var offers = await _db.PaymentOffers
                .AsNoTracking()
                .Where(offer => offer.IsActive &&
                    offer.Scope == PaymentOfferScopes.Membership &&
                    offer.ContentItemId == null)
                .OrderBy(offer => offer.Provider)
                .ThenBy(offer => offer.Id)
                .ToListAsync();

            return Ok(offers.Select(offer => offer.ToDto()));
        }

        [HttpPut("admin/{id:int}")]
        [Authorize(Policy = PermissionPolicies.PaymentsEditAll)]
        public async Task<IActionResult> UpdateForAdmin(
            int id,
            UpdatePaymentOfferRequest request,
            CancellationToken cancellationToken = default)
        {
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (offer == null)
            {
                return NotFound();
            }

            if (request == null)
            {
                return BadRequest(new { error = "Produktet mangler." });
            }

            if (request.IsActive == false && IsMembershipScope(offer))
            {
                return BadRequest(new { error = "Medlemskabsprodukter kan ikke deaktiveres." });
            }

            var originalOffer = PaymentOfferSnapshot.Capture(offer);
            var originalSubscriptions = await _db.PaymentSubscriptions
                .Where(item => item.PaymentOfferId == offer.Id)
                .ToListAsync(cancellationToken);
            var originalSubscriptionState = originalSubscriptions
                .Select(PaymentSubscriptionSnapshot.Capture)
                .ToList();
            var previousProviderPriceId = offer.ProviderPriceId;
            IDbContextTransaction transaction = null;
            var providerChangesApplied = false;
            var mobilePayProviderChangesStarted = false;
            var rescheduledMobilePayCharges = new System.Collections.Generic.List<MobilePayChargeReplacement>();

            try
            {
                transaction = await BeginOfferTransactionAsync(cancellationToken);
                var previousAmountMinor = offer.AmountMinor;
                var previousBillingInterval = offer.BillingInterval;
                var previousBillingIntervalCount = offer.BillingIntervalCount;
                var previousProductName = offer.ProductName;
                offer.LinkLabel = Required(request?.LinkLabel, "Link label");
                offer.ProductName = Required(request?.ProductName, "Produktnavn");

                if (request.Amount.HasValue)
                {
                    offer.AmountMinor = ToMinorUnits(request.Amount.Value);
                }

                var amountChanged = request.Amount.HasValue && previousAmountMinor != offer.AmountMinor;

                if (string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase))
                {
                    offer.BillingInterval = string.IsNullOrWhiteSpace(request.BillingInterval)
                        ? offer.BillingInterval
                        : request.BillingInterval.Trim().ToUpperInvariant();
                    offer.BillingIntervalCount = request.BillingIntervalCount ?? offer.BillingIntervalCount;
                }
                else
                {
                    offer.BillingInterval = null;
                    offer.BillingIntervalCount = null;
                }

                var recurringDefinitionChanged =
                    amountChanged ||
                    !string.Equals(previousBillingInterval, offer.BillingInterval, StringComparison.OrdinalIgnoreCase) ||
                    previousBillingIntervalCount != offer.BillingIntervalCount;
                var stripeDefinitionChanged = recurringDefinitionChanged ||
                    !string.Equals(previousProductName, offer.ProductName, StringComparison.Ordinal);

                if (request.IsActive.HasValue)
                {
                    offer.IsActive = request.IsActive.Value;
                }

                // Persist the new local definition before touching a provider.
                // The transaction keeps it invisible to other requests until
                // all provider work has completed successfully.
                await _db.SaveChangesAsync(cancellationToken);

                if (amountChanged &&
                    string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase))
                {
                    mobilePayProviderChangesStarted = true;
                    await UpdateMobilePayAgreementAmountsAsync(
                        offer,
                        previousAmountMinor.GetValueOrDefault(),
                        offer.AmountMinor.Value,
                        rescheduledMobilePayCharges,
                        cancellationToken);
                }

                if (stripeDefinitionChanged &&
                    string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase))
                    {
                        await UpdateStripeSubscriptionPricesAsync(offer, cancellationToken);
                    }
                    else
                    {
                        await EnsureStripeOfferReferencesAsync(offer);
                    }
                }

                providerChangesApplied = true;

                offer.Updated = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                if (string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase))
                {
                    await TryArchiveReplacedStripePriceAsync(offer, previousProviderPriceId, cancellationToken);
                }

                return Ok(offer.ToDto());
            }
            catch (ArgumentException ex)
            {
                await RollbackOfferChangeAsync(
                    transaction,
                    offer,
                    originalOffer,
                    originalSubscriptionState,
                    cancellationToken);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception exception) when (IsMobilePayProviderFailure(exception, cancellationToken))
            {
                if (providerChangesApplied || mobilePayProviderChangesStarted)
                {
                    await TryCompensateProviderChangesAsync(
                        offer,
                        originalOffer,
                        originalSubscriptionState,
                        rescheduledMobilePayCharges);
                }

                if (!providerChangesApplied &&
                    !string.Equals(offer.ProviderPriceId, originalOffer.ProviderPriceId, StringComparison.Ordinal) &&
                    string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase))
                {
                    await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                }

                await RollbackOfferChangeAsync(
                    transaction,
                    offer,
                    originalOffer,
                    originalSubscriptionState,
                    cancellationToken);
                return StatusCode(502, new { error = "Betalingstilbuddet kunne ikke opdateres hos betalingsudbyderen." });
            }
            catch (DbUpdateException)
            {
                if (providerChangesApplied || mobilePayProviderChangesStarted)
                {
                    await TryCompensateProviderChangesAsync(
                        offer,
                        originalOffer,
                        originalSubscriptionState,
                        rescheduledMobilePayCharges);
                }

                if (!providerChangesApplied &&
                    !string.Equals(offer.ProviderPriceId, originalOffer.ProviderPriceId, StringComparison.Ordinal) &&
                    string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase))
                {
                    await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                }

                await RollbackOfferChangeAsync(
                    transaction,
                    offer,
                    originalOffer,
                    originalSubscriptionState,
                    cancellationToken);
                return StatusCode(409, new { error = "Betalingstilbuddet kunne ikke gemmes." });
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private async Task UpdateMobilePayAgreementAmountsAsync(
            PaymentOffer offer,
            long previousAmountMinor,
            long amountMinor,
            System.Collections.Generic.ICollection<MobilePayChargeReplacement> rescheduledCharges,
            CancellationToken cancellationToken)
        {
            var subscriptions = await _db.PaymentSubscriptions
                .Where(subscription =>
                    subscription.Provider == PaymentOfferProviders.MobilePay &&
                    subscription.PaymentOfferId == offer.Id &&
                    !string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId) &&
                    !EF.Functions.Like(subscription.ProviderSubscriptionId, "pending:%") &&
                    subscription.Status == PaymentSubscriptionStatuses.Active)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
            {
                return;
            }

            if (_mobilePayService == null)
            {
                throw new InvalidOperationException("MobilePay-tjenesten er ikke konfigureret.");
            }

            var updatedSubscriptions = new System.Collections.Generic.List<PaymentSubscription>();
            try
            {
                foreach (var subscription in subscriptions)
                {
                    await ExecuteMobilePayWithRetryAsync(
                        () => _mobilePayService.UpdateAgreementAmountAsync(
                            subscription.ProviderSubscriptionId,
                            amountMinor,
                            cancellationToken),
                        cancellationToken);
                    updatedSubscriptions.Add(subscription);

                    var charges = await ExecuteMobilePayWithRetryAsync(
                        () => _mobilePayService.GetRecurringChargesAsync(
                            subscription.ProviderSubscriptionId,
                            cancellationToken),
                        cancellationToken) ?? Array.Empty<MobilePayCharge>();

                    foreach (var charge in charges.Where(IsFuturePendingRecurringCharge))
                    {
                        var due = charge.Due.Value;
                        var description = offer.ProductName;
                        var replacementChargeId = CreateRescheduledChargeId(
                            subscription.ProviderSubscriptionId,
                            charge.Id,
                            amountMinor,
                            description,
                            due,
                            "replace");

                        await ExecuteMobilePayWithRetryAsync(
                            () => _mobilePayService.CancelRecurringChargeAsync(
                                subscription.ProviderSubscriptionId,
                                charge.Id,
                                cancellationToken),
                            cancellationToken);

                        try
                        {
                            await ExecuteMobilePayWithRetryAsync(
                                () => _mobilePayService.CreateRecurringChargeAsync(
                                    subscription.ProviderSubscriptionId,
                                    replacementChargeId,
                                    amountMinor,
                                    offer.Currency ?? Currency,
                                    description,
                                    due,
                                    cancellationToken,
                                    charge.RetryDays,
                                    charge.TransactionType),
                                cancellationToken);
                        }
                        catch
                        {
                            try
                            {
                                await ExecuteMobilePayWithRetryAsync(
                                    () => _mobilePayService.CreateRecurringChargeAsync(
                                        subscription.ProviderSubscriptionId,
                                        CreateRescheduledChargeId(
                                            subscription.ProviderSubscriptionId,
                                            charge.Id,
                                            charge.AmountMinor,
                                            charge.Description,
                                            due,
                                            "restore"),
                                        charge.AmountMinor,
                                        charge.Currency ?? offer.Currency ?? Currency,
                                        charge.Description ?? offer.ProductName,
                                        due,
                                        cancellationToken,
                                        charge.RetryDays,
                                        charge.TransactionType),
                                    cancellationToken);
                            }
                            catch (Exception restoreException) when (IsMobilePayProviderFailure(restoreException, cancellationToken))
                            {
                                _logger?.LogError(
                                    restoreException,
                                    "Could not restore MobilePay charge {ChargeId} after replacement failed for agreement {AgreementId}.",
                                    charge.Id,
                                    subscription.ProviderSubscriptionId);
                            }

                            throw;
                        }

                        rescheduledCharges.Add(new MobilePayChargeReplacement(
                            subscription.ProviderSubscriptionId,
                            charge.Id,
                            charge.AmountMinor,
                            charge.Currency ?? offer.Currency ?? Currency,
                            charge.Description ?? offer.ProductName,
                            due,
                            replacementChargeId,
                            charge.RetryDays,
                            charge.TransactionType));
                    }
                }
            }
            catch
            {
                foreach (var subscription in updatedSubscriptions)
                {
                    try
                    {
                        await ExecuteMobilePayWithRetryAsync(
                            () => _mobilePayService.UpdateAgreementAmountAsync(
                                subscription.ProviderSubscriptionId,
                                previousAmountMinor,
                                cancellationToken),
                            cancellationToken);
                    }
                    catch (Exception restoreException) when (IsMobilePayProviderFailure(restoreException, cancellationToken))
                    {
                        // The original failure is the actionable response. A
                        // later reconciliation can repair an unavailable provider.
                        _logger?.LogError(
                            restoreException,
                            "Could not restore MobilePay agreement {AgreementId} after charge rescheduling failed.",
                            subscription.ProviderSubscriptionId);
                    }
                }

                throw;
            }
        }

        private static bool IsFuturePendingRecurringCharge(MobilePayCharge charge)
        {
            return charge != null &&
                string.Equals(charge.Status, "PENDING", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(charge.Type, "RECURRING", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(charge.Id) &&
                charge.Due.HasValue &&
                charge.Due.Value > DateTime.UtcNow;
        }

        private static bool IsMobilePayProviderFailure(Exception exception, CancellationToken cancellationToken)
        {
            return exception is InvalidOperationException ||
                exception is HttpRequestException ||
                (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
        }

        private async Task ExecuteMobilePayWithRetryAsync(
            Func<Task> operation,
            CancellationToken cancellationToken)
        {
            await ExecuteMobilePayWithRetryAsync<object>(async () =>
            {
                await operation();
                return null;
            }, cancellationToken);
        }

        private async Task<T> ExecuteMobilePayWithRetryAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken)
        {
            const int maxAttempts = 3;

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception exception) when (IsMobilePayProviderFailure(exception, cancellationToken) && attempt < maxAttempts)
                {
                    _logger?.LogWarning(
                        exception,
                        "MobilePay operation failed on attempt {Attempt} of {MaxAttempts}; retrying.",
                        attempt,
                        maxAttempts);
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
                }
            }
        }

        private async Task UpdateStripeSubscriptionPricesAsync(
            PaymentOffer offer,
            CancellationToken cancellationToken)
        {
            var previousPriceId = offer.ProviderPriceId;
            await EnsureStripeOfferReferencesAsync(offer);

            if (string.IsNullOrWhiteSpace(offer.ProviderPriceId))
            {
                throw new InvalidOperationException("Stripe-prisen kunne ikke oprettes.");
            }

            var subscriptions = await _db.PaymentSubscriptions
                .Where(subscription =>
                    subscription.Provider == PaymentOfferProviders.Stripe &&
                    subscription.PaymentOfferId == offer.Id &&
                    (subscription.Status == PaymentSubscriptionStatuses.Pending ||
                     subscription.Status == PaymentSubscriptionStatuses.Active ||
                     subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                     subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                     subscription.Status == PaymentSubscriptionStatuses.Incomplete))
                .ToListAsync(cancellationToken);

            var updatedSubscriptions = new System.Collections.Generic.List<PaymentSubscription>();
            try
            {
                foreach (var subscription in subscriptions)
                {
                    if (string.Equals(subscription.ProviderPriceId, offer.ProviderPriceId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!await _stripeService.UpdateSubscriptionPriceAsync(
                            subscription.ProviderSubscriptionId,
                            offer.ProviderPriceId))
                    {
                        throw new InvalidOperationException(
                            $"Stripe-abonnementet {subscription.ProviderSubscriptionId} kunne ikke opdateres.");
                    }

                    subscription.ProviderPriceId = offer.ProviderPriceId;
                    subscription.ProviderProductId = offer.ProviderProductId;
                    subscription.Updated = DateTime.UtcNow;
                    updatedSubscriptions.Add(subscription);
                }
            }
            catch
            {
                foreach (var subscription in updatedSubscriptions)
                {
                    try
                    {
                        await _stripeService.UpdateSubscriptionPriceAsync(
                            subscription.ProviderSubscriptionId,
                            previousPriceId);
                    }
                    catch (InvalidOperationException)
                    {
                        // Keep the original provider failure as the response.
                    }

                    subscription.ProviderPriceId = previousPriceId;
                }

                if (!string.Equals(previousPriceId, offer.ProviderPriceId, StringComparison.Ordinal))
                {
                    await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                }

                throw;
            }
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        {
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .ThenInclude(item => item.Links)
                .FirstOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);

            if (offer == null || !IsEventOffer(offer) || !IsEventPaymentOpen(offer.ContentItem))
            {
                return NotFound();
            }

            // Loading the payment page must also work for users who followed a
            // payment link before registering. Checkout itself still enforces
            // the participant and already-paid guards below.
            return Ok(offer.ToDto());
        }

        [HttpGet("success-context")]
        [Authorize]
        [EnableRateLimiting("payment-checkout")]
        public async Task<IActionResult> GetSuccessContext(string session)
        {
            if (string.IsNullOrWhiteSpace(session))
            {
                return BadRequest("Betalingssessionen mangler.");
            }

            var stripeSession = await _stripeService.GetSessionAsync(session);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (stripeSession == null ||
                !_stripeService.IsPaymentComplete(stripeSession) ||
                string.IsNullOrWhiteSpace(userId) ||
                !string.Equals(stripeSession.ClientReferenceId, userId, StringComparison.Ordinal))
            {
                return NotFound();
            }

            var paymentOfferId = MetadataValue(stripeSession.Metadata, "PaymentOfferId");
            var providerProductId = MetadataValue(stripeSession.Metadata, "Product");
            var productName = MetadataValue(stripeSession.Metadata, "ProductName");
            PaymentOffer offer = null;
            var parsedOfferId = 0;

            if (int.TryParse(paymentOfferId, out parsedOfferId))
            {
                offer = await _db.PaymentOffers
                    .Include(item => item.ContentItem)
                    .FirstOrDefaultAsync(item => item.Id == parsedOfferId);
            }

            if (offer == null && !string.IsNullOrWhiteSpace(providerProductId))
            {
                offer = await _db.PaymentOffers
                    .Include(item => item.ContentItem)
                    .Where(item => item.IsActive && item.ProviderProductId == providerProductId &&
                        (string.IsNullOrWhiteSpace(productName) || item.ProductName == productName))
                    .OrderByDescending(item => item.Updated)
                    .FirstOrDefaultAsync();
            }

            if (offer == null &&
                int.TryParse(MetadataValue(stripeSession.Metadata, "ContentItemId"), out var contentItemId))
            {
                var deletedOfferEvent = await _db.ContentItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == contentItemId && item.Type == ContentTypes.Event);
                if (deletedOfferEvent != null && !string.IsNullOrWhiteSpace(deletedOfferEvent.Slug))
                {
                    var deletedOfferRegistration = await _db.EventRegistrations
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.UserId == userId);
                    return Ok(new
                    {
                        type = "EVENT",
                        status = deletedOfferRegistration?.Registered == true ? "CONFIRMED" : "PENDING",
                        eventSlug = deletedOfferEvent.Slug,
                        offerId = parsedOfferId
                    });
                }
            }

            if (offer != null &&
                string.Equals(offer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(stripeSession.Mode, "payment", StringComparison.OrdinalIgnoreCase))
            {
                var membershipIsConfirmed = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == userId && item.Status == UserStatus.BETALT, HttpContext.RequestAborted);
                return Ok(new
                {
                    type = "MEMBERSHIP",
                    status = membershipIsConfirmed ? "CONFIRMED" : "PENDING"
                });
            }

            if (offer?.ContentItem?.Type != ContentTypes.Event || string.IsNullOrWhiteSpace(offer.ContentItem.Slug))
            {
                var membershipIsConfirmed = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == userId && item.Status == UserStatus.BETALT, HttpContext.RequestAborted);
                return Ok(new
                {
                    type = "MEMBERSHIP",
                    status = membershipIsConfirmed ? "CONFIRMED" : "PENDING"
                });
            }

            var registration = await _db.EventRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ContentItemId == offer.ContentItemId && item.UserId == userId);
            return Ok(new
            {
                type = "EVENT",
                status = registration?.Registered == true ? "CONFIRMED" : "PENDING",
                eventSlug = offer.ContentItem.Slug,
                offerId = offer.Id
            });
        }

        [HttpPost("event/{contentItemId:int}")]
        [Authorize]
        public async Task<IActionResult> SaveForEvent(
            int contentItemId,
            SavePaymentOfferRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!await _accessControl.CanEditEventAsync(User, contentItemId))
            {
                return Forbid();
            }

            var item = await _db.ContentItems
                .Include(content => content.Links)
                .Include(content => content.PaymentOffers)
                .FirstOrDefaultAsync(content => content.Id == contentItemId);

            if (item == null)
            {
                return NotFound();
            }

            if (!string.Equals(item.Type, ContentTypes.Event, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Betalingstilbud kan kun oprettes for events.");
            }

            PaymentOffer offer = null;
            PaymentOfferSnapshot originalOffer = null;
            var originalLinks = new System.Collections.Generic.List<ContentLink>();
            IDbContextTransaction transaction = null;
            var providerChangesApplied = false;

            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Betalingstilbud mangler." });
                }

                var provider = NormalizeProvider(request?.Provider);
                var productName = Required(request?.ProductName, "Produktnavn");
                var linkLabel = Required(request?.LinkLabel, "Link label");
                var amountMinor = ToMinorUnits(request.Amount);
                offer = item.PaymentOffers.FirstOrDefault(current =>
                    current.Provider == provider && current.OfferType == PaymentOfferTypes.OneTime);
                originalOffer = offer == null ? null : PaymentOfferSnapshot.Capture(offer);

                if (offer == null)
                {
                    offer = new PaymentOffer
                    {
                        ContentItemId = item.Id,
                        Scope = PaymentOfferScopes.Event,
                        Provider = provider,
                        OfferType = PaymentOfferTypes.OneTime,
                        Created = DateTime.UtcNow
                    };
                    _db.PaymentOffers.Add(offer);
                }

                offer.ProductName = productName;
                offer.LinkLabel = linkLabel;
                offer.Scope = PaymentOfferScopes.Event;
                offer.AmountMinor = amountMinor;
                offer.Currency = Currency;
                offer.IsActive = true;
                offer.Updated = DateTime.UtcNow;

                originalLinks = item.Links
                    .Where(link => IsLegacyPaymentLink(link, offer.Id))
                    .ToList();
                _db.ContentLinks.RemoveRange(originalLinks);

                // Persist the local definition first. Provider calls happen
                // only after this state is safely inside the local transaction.
                transaction = await BeginOfferTransactionAsync(cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                if (provider == PaymentOfferProviders.Stripe)
                {
                    await EnsureStripeReferencesAsync(offer, item.Title, productName, amountMinor);
                }
                else
                {
                    offer.ProviderProductId = null;
                    offer.ProviderPriceId = null;
                    offer.ProviderPaymentLinkId = null;
                    offer.ProviderPaymentUrl = null;
                }

                providerChangesApplied = true;

                await _db.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                if (provider == PaymentOfferProviders.Stripe)
                {
                    await TryArchiveReplacedStripePriceAsync(
                        offer,
                        originalOffer?.ProviderPriceId,
                        cancellationToken);
                }

                return Ok(offer.ToDto(item));
            }
            catch (ArgumentException ex)
            {
                await RollbackEventOfferChangeAsync(
                    transaction,
                    offer,
                    originalOffer,
                    originalLinks,
                    cancellationToken);
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException)
            {
                if (providerChangesApplied &&
                    providerIsStripe(offer) &&
                    !string.Equals(offer.ProviderPriceId, originalOffer?.ProviderPriceId, StringComparison.Ordinal))
                {
                    await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                }

                if (offer != null &&
                    !providerChangesApplied &&
                    !string.Equals(offer.ProviderPriceId, originalOffer?.ProviderPriceId, StringComparison.Ordinal) &&
                    providerIsStripe(offer))
                {
                    await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                }

                await RollbackEventOfferChangeAsync(
                    transaction,
                    offer,
                    originalOffer,
                    originalLinks,
                    cancellationToken);
                return StatusCode(502, new { error = "Betalingstilbuddet kunne ikke slettes hos betalingsudbyderen." });
            }
            catch (DbUpdateException)
            {
                if (providerChangesApplied &&
                    providerIsStripe(offer) &&
                    !string.Equals(offer.ProviderPriceId, originalOffer?.ProviderPriceId, StringComparison.Ordinal))
                {
                    await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                }

                await RollbackEventOfferChangeAsync(
                    transaction,
                    offer,
                    originalOffer,
                    originalLinks,
                    cancellationToken);
                return StatusCode(409, new { error = "Betalingstilbuddet kunne ikke gemmes." });
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }

            static bool providerIsStripe(PaymentOffer value) =>
                string.Equals(value.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase);
        }

        [HttpPost("{id:int}/checkout")]
        [Authorize]
        [EnableRateLimiting("payment-checkout")]
        public async Task<IActionResult> Checkout(int id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .FirstOrDefaultAsync(item => item.Id == id && item.IsActive);

            if (offer == null)
            {
                return NotFound();
            }

            if (IsMembershipOffer(offer))
            {
                if (await HasPaidMembershipAsync(userId, cancellationToken))
                {
                    return Conflict("Du har allerede betalt medlemskab.");
                }

                if (string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(offer.OfferType, PaymentOfferTypes.OneTime, StringComparison.OrdinalIgnoreCase) &&
                    await HasOpenMembershipSubscriptionAsync(userId, cancellationToken))
                {
                    await AbandonPendingMobilePayMembershipsAsync(userId, cancellationToken);
                }

                if (string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(offer.OfferType, PaymentOfferTypes.OneTime, StringComparison.OrdinalIgnoreCase) &&
                    await HasOpenMembershipSubscriptionAsync(userId, cancellationToken))
                {
                    return Conflict("Du har allerede et aktivt eller afventende medlemskab.");
                }

                if (string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase))
                {
                    return await CheckoutStripeMembershipAsync(offer, userId, cancellationToken);
                }

                if (string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase))
                {
                    return await CheckoutMobilePayRecurringMembershipAsync(offer, userId, cancellationToken);
                }

                return await CheckoutMembershipAsync(offer, userId, cancellationToken);
            }

            if (!IsEventOffer(offer) || !IsEventPaymentOpen(offer.ContentItem))
            {
                return NotFound();
            }

            var registration = await _db.EventRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ContentItemId == offer.ContentItemId && item.UserId == userId);

            if (registration == null ||
                registration.Registered ||
                !IsEventParticipant(registration.RegistrationType))
            {
                return Forbid();
            }

            if (offer.OfferType != PaymentOfferTypes.OneTime)
            {
                return BadRequest("Denne betalingsudbyder kan ikke gennemføre betaling endnu.");
            }

            if (offer.Provider == PaymentOfferProviders.MobilePay)
            {
                if (!offer.AmountMinor.HasValue || offer.AmountMinor.Value <= 0 ||
                    !string.Equals(offer.Currency, Currency, StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict("Betalingstilbuddet stemmer ikke længere overens med MobilePay.");
                }

                var attemptKey = GetPaymentAttemptKey(userId, offer);
                var reference = _mobilePayReferenceService.Create(registration.Id, offer.Id, attemptKey);
                var mobilePayEventPath = $"/events/{Uri.EscapeDataString(offer.ContentItem.Slug)}/pay";
                var returnUrl = PublicUrl($"{mobilePayEventPath}/success?reference={Uri.EscapeDataString(reference)}");
                try
                {
                    var payment = await _mobilePayService.CreateOneTimePaymentAsync(
                        reference,
                        offer.AmountMinor.Value,
                        offer.Currency,
                        offer.ProductName,
                        returnUrl,
                        cancellationToken);
                    return Ok(new { provider = PaymentOfferProviders.MobilePay, url = payment.RedirectUrl });
                }
                catch (InvalidOperationException)
                {
                    return StatusCode(502, new { error = "MobilePay-betalingen kunne ikke startes." });
                }
            }

            if (offer.Provider != PaymentOfferProviders.Stripe ||
                string.IsNullOrWhiteSpace(offer.ProviderProductId) ||
                string.IsNullOrWhiteSpace(offer.ProviderPriceId))
            {
                return BadRequest("Denne betalingsudbyder kan ikke gennemføre betaling endnu.");
            }

            var product = await _stripeService.GetProductAsync(offer.ProviderProductId);
            var price = await _stripeService.GetPriceByIdAsync(offer.ProviderPriceId);
            if (product == null || price == null ||
                price.ProductId != product.Id ||
                price.Type != "one_time" ||
                price.UnitAmount != offer.AmountMinor ||
                !string.Equals(price.Currency, offer.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict("Betalingstilbuddet stemmer ikke længere overens med Stripe.");
            }

            var sessionParameter = "?session={CHECKOUT_SESSION_ID}";
            var eventPath = $"/events/{Uri.EscapeDataString(offer.ContentItem.Slug)}/pay";
            var successUrl = PublicUrl($"{eventPath}/success{sessionParameter}");
            var cancelUrl = PublicUrl($"{eventPath}/{offer.Id}");
            try
            {
                var sessionId = _stripeService.StartPayment(
                    product,
                    price,
                    userId,
                    successUrl,
                    cancelUrl,
                    "payment",
                    new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["PaymentOfferId"] = offer.Id.ToString(),
                        ["ContentItemId"] = offer.ContentItemId.ToString(),
                        ["GamMaUserId"] = userId
                    },
                    GetStripeIdempotencyKey(userId, offer));

                return Ok(new { id = sessionId });
            }
            catch (Stripe.StripeException)
            {
                return StatusCode(502, new { error = "Stripe-betalingen kunne ikke startes." });
            }
        }

        [HttpGet("mobilepay-context")]
        [Authorize]
        [EnableRateLimiting("payment-provider-read")]
        public async Task<IActionResult> GetMobilePayContext(string reference, CancellationToken cancellationToken)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (_mobilePayReferenceService.TryParseMembership(reference, out var membershipUserId, out var membershipOfferId))
            {
                if (!string.Equals(currentUserId, membershipUserId, StringComparison.Ordinal))
                {
                    return NotFound();
                }

                return await GetMembershipMobilePayContextAsync(
                    reference,
                    membershipUserId,
                    membershipOfferId,
                    cancellationToken);
            }

            if (!_mobilePayReferenceService.TryParse(reference, out var registrationId, out var offerId))
            {
                return NotFound();
            }

            var registration = await _db.EventRegistrations
                .Include(item => item.ContentItem)
                .FirstOrDefaultAsync(item => item.Id == registrationId && item.UserId == currentUserId, cancellationToken);
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .FirstOrDefaultAsync(item => item.Id == offerId && item.IsActive, cancellationToken);

            if (registration == null || offer == null ||
                registration.ContentItemId != offer.ContentItemId ||
                offer.ContentItem?.Type != ContentTypes.Event ||
                !string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            try
            {
                var payment = await _mobilePayService.GetPaymentAsync(reference, cancellationToken);
                if (registration.Registered)
                {
                    return Ok(new { type = "EVENT", status = "SUCCESS", eventSlug = offer.ContentItem.Slug, offerId });
                }

                if (payment.CapturedAmountMinor >= offer.AmountMinor.GetValueOrDefault())
                {
                    return Ok(new { type = "EVENT", status = "PENDING", eventSlug = offer.ContentItem.Slug, offerId });
                }

                if (string.Equals(payment.State, "AUTHORIZED", StringComparison.OrdinalIgnoreCase))
                {
                    await _mobilePayService.CaptureAsync(reference, offer.AmountMinor.GetValueOrDefault(), offer.Currency, cancellationToken);
                    return Ok(new { type = "EVENT", status = "PENDING", eventSlug = offer.ContentItem.Slug, offerId });
                }

                var isCancelled = string.Equals(payment.State, "ABORTED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(payment.State, "EXPIRED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(payment.State, "CANCELLED", StringComparison.OrdinalIgnoreCase);
                return Ok(new
                {
                    type = "EVENT",
                    status = isCancelled ? "CANCELLED" : "PENDING",
                    eventSlug = offer.ContentItem.Slug,
                    offerId
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(502, new { error = "MobilePay-betalingen kunne ikke kontrolleres." });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .ThenInclude(item => item.Links)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (offer == null)
            {
                return NotFound();
            }

            if (IsMembershipScope(offer))
            {
                return BadRequest("Medlemskabsprodukter kan ikke slettes.");
            }

            if (!IsEventOffer(offer) || !await _accessControl.CanEditEventAsync(User, offer.ContentItemId.Value))
            {
                return Forbid();
            }

            var paymentLinks = offer.ContentItem?.Links?
                .Where(link => string.Equals(link.Type, "PAYMENT", StringComparison.OrdinalIgnoreCase)
                    && IsLegacyPaymentLink(link, offer.Id))
                .ToList() ?? new();

            IDbContextTransaction transaction = null;
            var providerPriceDeactivated = false;
            try
            {
                transaction = await BeginOfferTransactionAsync(default);

                // Delete locally first. A relational transaction keeps the
                // deletion invisible until the provider operation succeeds.
                _db.ContentLinks.RemoveRange(paymentLinks);
                _db.PaymentOffers.Remove(offer);
                await _db.SaveChangesAsync();

                if (offer.Provider == PaymentOfferProviders.Stripe && !string.IsNullOrWhiteSpace(offer.ProviderPriceId))
                {
                    var priceIsUsedElsewhere = await _db.PaymentOffers
                        .AnyAsync(item => item.ProviderPriceId == offer.ProviderPriceId);

                    if (!priceIsUsedElsewhere)
                    {
                        providerPriceDeactivated = await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                        if (!providerPriceDeactivated)
                        {
                            throw new InvalidOperationException("Stripe-prisen kunne ikke deaktiveres.");
                        }
                    }
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return NoContent();
            }
            catch (InvalidOperationException)
            {
                if (providerPriceDeactivated)
                {
                    await _stripeService.SetPriceActiveAsync(offer.ProviderPriceId, true);
                }

                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                else
                {
                    _db.ChangeTracker.Clear();
                    offer.ContentItem = null;
                    _db.PaymentOffers.Add(offer);
                    foreach (var paymentLink in paymentLinks)
                    {
                        paymentLink.ContentItem = null;
                    }
                    _db.ContentLinks.AddRange(paymentLinks);
                    await _db.SaveChangesAsync();
                }

                return StatusCode(502, "Stripe-prisen kunne ikke deaktiveres. Betalingstilbuddet er ikke slettet.");
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private static bool IsLegacyPaymentLink(ContentLink link, int offerId)
        {
            return string.Equals(link.Type, "PAYMENT", StringComparison.OrdinalIgnoreCase)
                && link.Url?.EndsWith($"/pay/{offerId}", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsEventOffer(PaymentOffer offer)
        {
            return offer != null &&
                (string.IsNullOrWhiteSpace(offer.Scope) ||
                 string.Equals(offer.Scope, PaymentOfferScopes.Event, StringComparison.OrdinalIgnoreCase)) &&
                offer.ContentItemId.HasValue;
        }

        private static bool IsMembershipOffer(PaymentOffer offer)
        {
            return offer != null &&
                string.Equals(offer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase) &&
                !offer.ContentItemId.HasValue;
        }

        private static bool IsMembershipScope(PaymentOffer offer)
        {
            return offer != null &&
                string.Equals(offer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<IActionResult> CheckoutStripeMembershipAsync(
            PaymentOffer offer,
            string userId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                (offer.OfferType != PaymentOfferTypes.OneTime && offer.OfferType != PaymentOfferTypes.Recurring))
            {
                return BadRequest("Dette medlemskabstilbud kan ikke gennemføre betaling endnu.");
            }

            var hasActiveSubscription = await _db.PaymentSubscriptions.AnyAsync(subscription =>
                subscription.UserId == userId &&
                (subscription.Status == PaymentSubscriptionStatuses.Active ||
                 subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                 subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                 subscription.Status == PaymentSubscriptionStatuses.Incomplete), cancellationToken);
            if (hasActiveSubscription)
            {
                return Conflict("Du har allerede betalt medlemskab.");
            }

            if (!offer.AmountMinor.HasValue || offer.AmountMinor.Value <= 0 ||
                !string.Equals(offer.Currency, Currency, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict("Betalingstilbuddet stemmer ikke længere overens med Stripe.");
            }

            try
            {
                if (string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase))
                {
                    offer.BillingInterval ??= "YEAR";
                    offer.BillingIntervalCount ??= 1;
                }

                var previousPriceId = offer.ProviderPriceId;
                await EnsureStripeOfferReferencesAsync(offer);
                await _db.SaveChangesAsync(cancellationToken);
                await TryArchiveReplacedStripePriceAsync(offer, previousPriceId, cancellationToken);

                var product = await _stripeService.GetProductAsync(offer.ProviderProductId);
                var price = await _stripeService.GetPriceByIdAsync(offer.ProviderPriceId);
                if (!IsMatchingStripePrice(price, offer, product))
                {
                    return Conflict("Betalingstilbuddet stemmer ikke længere overens med Stripe.");
                }

                var sessionParameter = "?session={CHECKOUT_SESSION_ID}";
                var successUrl = PublicUrl($"/pay/success{sessionParameter}");
                var cancelUrl = PublicUrl("/pay/cancel");
                var mode = string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase)
                    ? "subscription"
                    : "payment";
                var sessionId = _stripeService.StartPayment(
                    product,
                    price,
                    userId,
                    successUrl,
                    cancelUrl,
                    mode,
                    new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["PaymentOfferId"] = offer.Id.ToString(),
                        ["Scope"] = PaymentOfferScopes.Membership,
                        ["GamMaUserId"] = userId
                    },
                    GetStripeIdempotencyKey(userId, offer));

                return Ok(new { provider = PaymentOfferProviders.Stripe, id = sessionId });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(502, new { error = "Stripe-betalingen kunne ikke startes." });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (Stripe.StripeException)
            {
                return StatusCode(502, new { error = "Stripe-betalingen kunne ikke startes." });
            }
        }

        private async Task<IActionResult> CheckoutMembershipAsync(
            PaymentOffer offer,
            string userId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId) || offer.OfferType != PaymentOfferTypes.OneTime)
            {
                return BadRequest("Dette medlemskabstilbud kan ikke gennemføre betaling endnu.");
            }

            if (offer.Provider != PaymentOfferProviders.MobilePay ||
                !offer.AmountMinor.HasValue || offer.AmountMinor.Value <= 0 ||
                !string.Equals(offer.Currency, Currency, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Dette MobilePay-medlemskab kan ikke gennemføre betaling endnu.");
            }

            var attemptKey = GetPaymentAttemptKey(userId, offer);
            var reference = _mobilePayReferenceService.CreateMembership(userId, offer.Id, attemptKey);
            var returnUrl = PublicUrl($"/pay/success?reference={Uri.EscapeDataString(reference)}");
            try
            {
                var payment = await _mobilePayService.CreateOneTimePaymentAsync(
                    reference,
                    offer.AmountMinor.Value,
                    offer.Currency,
                    offer.ProductName,
                    returnUrl,
                    cancellationToken);
                return Ok(new { provider = PaymentOfferProviders.MobilePay, url = payment.RedirectUrl });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(502, new { error = "MobilePay-medlemsbetalingen kunne ikke startes." });
            }
        }

        private async Task<IActionResult> CheckoutMobilePayRecurringMembershipAsync(
            PaymentOffer offer,
            string userId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                offer.Provider != PaymentOfferProviders.MobilePay ||
                offer.OfferType != PaymentOfferTypes.Recurring ||
                !offer.AmountMinor.HasValue || offer.AmountMinor.Value <= 0 ||
                !string.Equals(offer.Currency, Currency, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Dette MobilePay-abonnement kan ikke gennemføre betaling endnu.");
            }

            var member = await _db.Users.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (member == null || string.IsNullOrWhiteSpace(member.PhoneNumber))
            {
                return BadRequest("Der mangler et gyldigt telefonnummer til MobilePay-abonnementet.");
            }

            var openSubscription = await _db.PaymentSubscriptions
                .FirstOrDefaultAsync(subscription =>
                    subscription.UserId == userId &&
                    (subscription.Status == PaymentSubscriptionStatuses.Pending ||
                     subscription.Status == PaymentSubscriptionStatuses.Active ||
                     subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                     subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                     subscription.Status == PaymentSubscriptionStatuses.Incomplete), cancellationToken);
            if (openSubscription != null &&
                string.Equals(openSubscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                !IsPendingMobilePayAgreement(openSubscription))
            {
                openSubscription = await _mobilePayReconciliation.ReconcileAsync(
                    openSubscription,
                    cancellationToken);

                if (string.Equals(openSubscription.Status, PaymentSubscriptionStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                {
                    await _mobilePayReconciliation.TryAbandonPendingAsync(
                        openSubscription,
                        cancellationToken);
                    openSubscription = await _db.PaymentSubscriptions
                        .FirstOrDefaultAsync(subscription =>
                            subscription.UserId == userId &&
                            (subscription.Status == PaymentSubscriptionStatuses.Pending ||
                             subscription.Status == PaymentSubscriptionStatuses.Active ||
                             subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                             subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                             subscription.Status == PaymentSubscriptionStatuses.Incomplete),
                            cancellationToken);
                }
            }

            var hasOpenSubscription = await _db.PaymentSubscriptions.AnyAsync(subscription =>
                subscription.UserId == userId &&
                (subscription.Status == PaymentSubscriptionStatuses.Pending ||
                 subscription.Status == PaymentSubscriptionStatuses.Active ||
                 subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                 subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                 subscription.Status == PaymentSubscriptionStatuses.Incomplete), cancellationToken);
            var canResumePendingMobilePayAgreement = openSubscription != null &&
                IsPendingMobilePayAgreement(openSubscription) &&
                openSubscription.PaymentOfferId == offer.Id;
            if (hasOpenSubscription && !canResumePendingMobilePayAgreement)
            {
                return Conflict("Du har allerede et aktivt eller afventende medlemskab.");
            }

            var billingInterval = string.IsNullOrWhiteSpace(offer.BillingInterval)
                ? "YEAR"
                : offer.BillingInterval.Trim().ToUpperInvariant();
            var billingIntervalCount = offer.BillingIntervalCount.GetValueOrDefault(1);
            if (!IsSupportedMobilePayInterval(billingInterval) || billingIntervalCount <= 0)
            {
                return BadRequest("MobilePay-medlemskabet har et ugyldigt betalingsinterval.");
            }

            var attemptKey = GetPaymentAttemptKey(userId, offer);
            var merchantAgreementUrl = PublicUrl("/account/manage/subscriptions");
            try
            {
                var now = DateTime.UtcNow;
                var subscription = await _db.PaymentSubscriptions
                    .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
                var reference = canResumePendingMobilePayAgreement
                    ? GetPendingMobilePayReference(subscription.ProviderSubscriptionId)
                    : _mobilePayReferenceService.CreateMembership(userId, offer.Id, attemptKey);
                if (string.IsNullOrWhiteSpace(reference))
                {
                    return Conflict("Det afventende MobilePay-medlemskab kunne ikke genoptages.");
                }

                if (subscription == null)
                {
                    subscription = new PaymentSubscription
                    {
                        UserId = userId,
                        Created = now
                    };
                    _db.PaymentSubscriptions.Add(subscription);
                }

                // Reserve the user's unique subscription row before calling MobilePay.
                // This prevents concurrent requests from creating multiple agreements.
                subscription.Provider = PaymentOfferProviders.MobilePay;
                subscription.PaymentOfferId = offer.Id;
                subscription.ProviderSubscriptionId = PendingMobilePaySubscriptionId(reference);
                subscription.ProductName = offer.ProductName;
                subscription.ProviderProductId = offer.Id.ToString();
                subscription.ProviderPriceId = null;
                subscription.Status = PaymentSubscriptionStatuses.Pending;
                subscription.CurrentPeriodStart = null;
                subscription.CurrentPeriodEnd = null;
                subscription.NextChargeAt = null;
                subscription.LastPaidAt = null;
                subscription.CancelAtPeriodEnd = false;
                subscription.LastProcessedWebhookId = null;
                subscription.LastProcessedWebhookAt = null;
                subscription.Updated = now;
                await _db.SaveChangesAsync(cancellationToken);

                var merchantRedirectUrl =
                    PublicUrl($"/pay/success?reference={Uri.EscapeDataString(reference)}");
                var agreement = await _mobilePayService.CreateRecurringAgreementAsync(
                    reference,
                    member.PhoneNumber,
                    offer.AmountMinor.Value,
                    offer.Currency,
                    billingInterval,
                    billingIntervalCount,
                    offer.ProductName,
                    $"Automatisk betaling for {offer.ProductName}",
                    merchantRedirectUrl,
                    merchantAgreementUrl,
                    cancellationToken);

                subscription.ProviderSubscriptionId = agreement.AgreementId;
                subscription.LastProcessedWebhookId = null;
                subscription.LastProcessedWebhookAt = null;
                subscription.Updated = now;
                await _db.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    provider = PaymentOfferProviders.MobilePay,
                    url = agreement.RedirectUrl
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(502, new { error = "MobilePay kunne ikke starte betalingen." });
            }
            catch (DbUpdateException)
            {
                return Conflict("Medlemskabet kunne ikke gemmes. Prøv igen.");
            }
        }

        private async Task<IActionResult> GetMembershipMobilePayContextAsync(
            string reference,
            string userId,
            int offerId,
            CancellationToken cancellationToken)
        {
            var offer = await _db.PaymentOffers
                .FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken);

            if (offer == null || !IsMembershipOffer(offer) ||
                offer.Provider != PaymentOfferProviders.MobilePay ||
                (offer.OfferType != PaymentOfferTypes.OneTime && offer.OfferType != PaymentOfferTypes.Recurring))
            {
                return NotFound();
            }

            if (offer.OfferType == PaymentOfferTypes.Recurring)
            {
                return await GetMembershipMobilePayRecurringContextAsync(
                    reference,
                    userId,
                    offer,
                    cancellationToken);
            }

            try
            {
                var membershipIsConfirmed = await _db.Users
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == userId && item.Status == UserStatus.BETALT, cancellationToken);
                if (membershipIsConfirmed)
                {
                    return Ok(new { type = "MEMBERSHIP", status = "SUCCESS" });
                }

                var payment = await _mobilePayService.GetPaymentAsync(reference, cancellationToken);
                if (payment.CapturedAmountMinor >= offer.AmountMinor.GetValueOrDefault())
                {
                    return Ok(new { type = "MEMBERSHIP", status = "PENDING" });
                }

                if (string.Equals(payment.State, "AUTHORIZED", StringComparison.OrdinalIgnoreCase))
                {
                    await _mobilePayService.CaptureAsync(reference, offer.AmountMinor.GetValueOrDefault(), offer.Currency, cancellationToken);
                    return Ok(new { type = "MEMBERSHIP", status = "PENDING" });
                }

                var isCancelled = IsCancelledPaymentState(payment.State);
                return Ok(new
                {
                    type = "MEMBERSHIP",
                    status = isCancelled ? "CANCELLED" : "PENDING"
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(502, new { error = "MobilePay-betalingen kunne ikke kontrolleres." });
            }
        }

        private async Task<IActionResult> GetMembershipMobilePayRecurringContextAsync(
            string reference,
            string userId,
            PaymentOffer offer,
            CancellationToken cancellationToken)
        {
            var subscription = await _db.PaymentSubscriptions
                .Where(item => item.UserId == userId &&
                    item.Provider == PaymentOfferProviders.MobilePay &&
                    item.ProviderProductId == offer.Id.ToString())
                .OrderByDescending(item => item.Created)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription == null)
            {
                return NotFound();
            }

            subscription = await _mobilePayReconciliation.ReconcileAsync(
                subscription,
                cancellationToken);

            if (string.Equals(subscription.Status, PaymentSubscriptionStatuses.Active, StringComparison.OrdinalIgnoreCase) &&
                subscription.LastPaidAt.HasValue)
            {
                return Ok(new { type = "MEMBERSHIP", status = "SUCCESS" });
            }

            if (string.Equals(subscription.Status, PaymentSubscriptionStatuses.Canceled, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(subscription.Status, PaymentSubscriptionStatuses.Expired, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { type = "MEMBERSHIP", status = "CANCELLED" });
            }

            return Ok(new { type = "MEMBERSHIP", status = "PENDING" });
        }

        private async Task EnsureStripeReferencesAsync(
            PaymentOffer offer,
            string eventTitle,
            string productName,
            long amountMinor)
        {
            var product = !string.IsNullOrWhiteSpace(offer.ProviderProductId)
                ? await _stripeService.GetProductAsync(offer.ProviderProductId)
                : null;

            if (product == null || !string.Equals(product.Name, productName, StringComparison.Ordinal))
            {
                product = await _stripeService.GetProductByExactNameAsync(productName);
            }

            if (product == null)
            {
                product = await _stripeService.CreateProductAsync(productName, $"Betaling for eventet {eventTitle}.");
            }

            if (product == null)
            {
                throw new InvalidOperationException("Stripe-produktet kunne ikke oprettes.");
            }

            var price = !string.IsNullOrWhiteSpace(offer.ProviderPriceId)
                ? await _stripeService.GetPriceByIdAsync(offer.ProviderPriceId)
                : null;
            var priceWasCreated = false;
            if (price == null || price.ProductId != product.Id || price.UnitAmount != amountMinor ||
                price.Type != "one_time" || !string.Equals(price.Currency, Currency, StringComparison.OrdinalIgnoreCase))
            {
                price = await _stripeService.CreateOneTimePriceAsync(
                    product.Id,
                    amountMinor,
                    Currency,
                    GetStripePriceDescription(offer));
                priceWasCreated = true;
            }

            if (price == null)
            {
                throw new InvalidOperationException("Stripe-prisen kunne ikke oprettes.");
            }

            if (!priceWasCreated)
            {
                price = await EnsureStripePriceNicknameAsync(price, offer);
            }

            offer.ProviderProductId = product.Id;
            offer.ProviderPriceId = price.Id;
        }

        private async Task EnsureStripeOfferReferencesAsync(PaymentOffer offer)
        {
            var product = !string.IsNullOrWhiteSpace(offer.ProviderProductId)
                ? await _stripeService.GetProductAsync(offer.ProviderProductId)
                : null;

            if (product == null || !string.Equals(product.Name, offer.ProductName, StringComparison.Ordinal))
            {
                product = await _stripeService.GetProductByExactNameAsync(offer.ProductName);
            }

            if (product == null)
            {
                product = await _stripeService.CreateProductAsync(
                    offer.ProductName,
                    "Betaling for medlemskab.");
            }

            if (product == null)
            {
                throw new InvalidOperationException("Stripe-produktet kunne ikke oprettes.");
            }

            var price = !string.IsNullOrWhiteSpace(offer.ProviderPriceId)
                ? await _stripeService.GetPriceByIdAsync(offer.ProviderPriceId)
                : null;

            if (price == null)
            {
                price = await _stripeService.GetPriceAsync(product.Id);
            }

            var priceWasCreated = false;
            if (!IsMatchingStripePrice(price, offer, product))
            {
                price = string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase)
                    ? await _stripeService.CreateRecurringPriceAsync(
                        product.Id,
                        offer.AmountMinor.GetValueOrDefault(),
                        Currency,
                        ToStripeInterval(offer.BillingInterval),
                        offer.BillingIntervalCount.GetValueOrDefault(1),
                        GetStripePriceDescription(offer))
                    : await _stripeService.CreateOneTimePriceAsync(
                        product.Id,
                        offer.AmountMinor.GetValueOrDefault(),
                        Currency,
                        GetStripePriceDescription(offer));
                priceWasCreated = true;
            }

            if (price == null)
            {
                throw new InvalidOperationException("Stripe-prisen kunne ikke oprettes.");
            }

            if (!priceWasCreated)
            {
                price = await EnsureStripePriceNicknameAsync(price, offer);
            }

            offer.ProviderProductId = product.Id;
            offer.ProviderPriceId = price.Id;
        }

        private async Task<Stripe.Price> EnsureStripePriceNicknameAsync(
            Stripe.Price price,
            PaymentOffer offer)
        {
            var nickname = GetStripePriceDescription(offer);
            if (string.Equals(price.Nickname, nickname, StringComparison.Ordinal))
            {
                return price;
            }

            var updatedPrice = await _stripeService.UpdatePriceNicknameAsync(price.Id, nickname);
            if (updatedPrice == null)
            {
                throw new InvalidOperationException("Stripe-prisens beskrivelse kunne ikke opdateres.");
            }

            return updatedPrice;
        }

        private async Task<bool> TryArchiveReplacedStripePriceAsync(
            PaymentOffer offer,
            string previousPriceId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(previousPriceId) ||
                string.Equals(previousPriceId, offer.ProviderPriceId, StringComparison.Ordinal))
            {
                return true;
            }

            var priceIsUsedByAnotherOffer = await _db.PaymentOffers
                .AnyAsync(item => item.Id != offer.Id && item.ProviderPriceId == previousPriceId, cancellationToken);
            if (priceIsUsedByAnotherOffer)
            {
                return true;
            }

            // Archiving is cleanup, not part of the payment definition. If it
            // fails, keep both the local and provider definition usable and let
            // a later administrative run retry the cleanup.
            return await _stripeService.DeactivatePriceAsync(previousPriceId);
        }

        private Task<IDbContextTransaction> BeginOfferTransactionAsync(CancellationToken cancellationToken)
        {
            return _db.Database.IsRelational()
                ? _db.Database.BeginTransactionAsync(cancellationToken)
                : Task.FromResult<IDbContextTransaction>(null);
        }

        private async Task RollbackOfferChangeAsync(
            IDbContextTransaction transaction,
            PaymentOffer offer,
            PaymentOfferSnapshot originalOffer,
            System.Collections.Generic.IReadOnlyCollection<PaymentSubscriptionSnapshot> originalSubscriptions,
            CancellationToken cancellationToken)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            if (offer != null && originalOffer != null)
            {
                originalOffer.Restore(offer);
            }

            foreach (var subscription in originalSubscriptions)
            {
                subscription.Restore(_db);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task CompensateProviderChangesAsync(
            PaymentOffer offer,
            PaymentOfferSnapshot originalOffer,
            System.Collections.Generic.IReadOnlyCollection<PaymentSubscriptionSnapshot> originalSubscriptions,
            System.Collections.Generic.IReadOnlyCollection<MobilePayChargeReplacement> rescheduledCharges,
            CancellationToken cancellationToken)
        {
            if (string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                originalOffer.AmountMinor.HasValue &&
                offer.AmountMinor != originalOffer.AmountMinor)
            {
                foreach (var subscription in originalSubscriptions.Where(item =>
                             string.Equals(item.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                             IsOpenSubscriptionStatus(item.Status)))
                {
                    if (_mobilePayService == null)
                    {
                        break;
                    }

                    try
                    {
                        await ExecuteMobilePayWithRetryAsync(
                            () => _mobilePayService.UpdateAgreementAmountAsync(
                                subscription.ProviderSubscriptionId,
                                originalOffer.AmountMinor.Value,
                                cancellationToken),
                            cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(
                            exception,
                            "Could not restore MobilePay agreement {AgreementId} after payment-offer rollback.",
                            subscription.ProviderSubscriptionId);
                    }
                }

                foreach (var charge in rescheduledCharges ?? Array.Empty<MobilePayChargeReplacement>())
                {
                    try
                    {
                        await ExecuteMobilePayWithRetryAsync(
                            () => _mobilePayService.CancelRecurringChargeAsync(
                                charge.AgreementId,
                                charge.ReplacementChargeId,
                                cancellationToken),
                            cancellationToken);

                        await ExecuteMobilePayWithRetryAsync(
                            () => _mobilePayService.CreateRecurringChargeAsync(
                                charge.AgreementId,
                                CreateRescheduledChargeId(
                                    charge.AgreementId,
                                    charge.OriginalChargeId,
                                    charge.OriginalAmountMinor,
                                    charge.OriginalDescription,
                                    charge.Due,
                                    "restore"),
                                charge.OriginalAmountMinor,
                                charge.Currency,
                                charge.OriginalDescription,
                                charge.Due,
                                cancellationToken,
                                charge.RetryDays,
                                charge.TransactionType),
                            cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(
                            exception,
                            "Could not restore MobilePay charge {ChargeId} after payment-offer rollback.",
                            charge.OriginalChargeId);
                    }
                }
            }

            if (string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(offer.ProviderPriceId, originalOffer.ProviderPriceId, StringComparison.Ordinal))
            {
                foreach (var subscription in originalSubscriptions.Where(item =>
                             string.Equals(item.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase) &&
                             IsOpenSubscriptionStatus(item.Status) &&
                             !string.IsNullOrWhiteSpace(originalOffer.ProviderPriceId)))
                {
                    try
                    {
                        var restored = await _stripeService.UpdateSubscriptionPriceAsync(
                            subscription.ProviderSubscriptionId,
                            originalOffer.ProviderPriceId);
                        if (!restored)
                        {
                            _logger?.LogError(
                                "Could not restore Stripe subscription {SubscriptionId} after payment-offer rollback.",
                                subscription.ProviderSubscriptionId);
                        }
                    }
                    catch (InvalidOperationException exception)
                    {
                        _logger?.LogError(
                            exception,
                            "Could not restore Stripe subscription {SubscriptionId} after payment-offer rollback.",
                            subscription.ProviderSubscriptionId);
                    }
                }

                var deactivated = await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId);
                if (!deactivated)
                {
                    _logger?.LogError(
                        "Could not deactivate Stripe price {PriceId} after payment-offer rollback.",
                        offer.ProviderPriceId);
                }
            }
        }

        private async Task TryCompensateProviderChangesAsync(
            PaymentOffer offer,
            PaymentOfferSnapshot originalOffer,
            System.Collections.Generic.IReadOnlyCollection<PaymentSubscriptionSnapshot> originalSubscriptions,
            System.Collections.Generic.IReadOnlyCollection<MobilePayChargeReplacement> rescheduledCharges)
        {
            // A request cancellation should not prevent us from repairing a
            // provider change that already happened. Keep compensation bounded
            // and independent from the request lifetime.
            using var recoveryCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await CompensateProviderChangesAsync(
                    offer,
                    originalOffer,
                    originalSubscriptions,
                    rescheduledCharges,
                    recoveryCancellation.Token);
            }
            catch (Exception exception)
            {
                _logger?.LogError(
                    exception,
                    "Could not complete provider compensation for payment offer {PaymentOfferId}.",
                    offer?.Id);
            }
        }

        private static bool IsOpenSubscriptionStatus(string status)
        {
            return status == PaymentSubscriptionStatuses.Pending ||
                status == PaymentSubscriptionStatuses.Active ||
                status == PaymentSubscriptionStatuses.Trialing ||
                status == PaymentSubscriptionStatuses.PastDue ||
                status == PaymentSubscriptionStatuses.Incomplete;
        }

        private async Task RollbackEventOfferChangeAsync(
            IDbContextTransaction transaction,
            PaymentOffer offer,
            PaymentOfferSnapshot originalOffer,
            System.Collections.Generic.IReadOnlyCollection<ContentLink> originalLinks,
            CancellationToken cancellationToken)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            _db.ChangeTracker.Clear();
            if (offer != null && originalOffer != null)
            {
                originalOffer.Restore(offer);
                _db.PaymentOffers.Update(offer);
            }
            else if (offer != null)
            {
                _db.PaymentOffers.Remove(offer);
            }

            if (originalLinks != null && originalLinks.Count > 0)
            {
                _db.ContentLinks.AddRange(originalLinks);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private sealed class PaymentOfferSnapshot
        {
            public int? ContentItemId { get; init; }
            public string Scope { get; init; }
            public string Provider { get; init; }
            public string OfferType { get; init; }
            public string LinkLabel { get; init; }
            public string ProductName { get; init; }
            public string ProviderProductId { get; init; }
            public string ProviderPriceId { get; init; }
            public string ProviderPaymentLinkId { get; init; }
            public string ProviderPaymentUrl { get; init; }
            public long? AmountMinor { get; init; }
            public string Currency { get; init; }
            public string BillingInterval { get; init; }
            public int? BillingIntervalCount { get; init; }
            public bool IsActive { get; init; }
            public DateTime Created { get; init; }
            public DateTime Updated { get; init; }

            public static PaymentOfferSnapshot Capture(PaymentOffer offer) => new()
            {
                ContentItemId = offer.ContentItemId,
                Scope = offer.Scope,
                Provider = offer.Provider,
                OfferType = offer.OfferType,
                LinkLabel = offer.LinkLabel,
                ProductName = offer.ProductName,
                ProviderProductId = offer.ProviderProductId,
                ProviderPriceId = offer.ProviderPriceId,
                ProviderPaymentLinkId = offer.ProviderPaymentLinkId,
                ProviderPaymentUrl = offer.ProviderPaymentUrl,
                AmountMinor = offer.AmountMinor,
                Currency = offer.Currency,
                BillingInterval = offer.BillingInterval,
                BillingIntervalCount = offer.BillingIntervalCount,
                IsActive = offer.IsActive,
                Created = offer.Created,
                Updated = offer.Updated
            };

            public void Restore(PaymentOffer offer)
            {
                offer.ContentItemId = ContentItemId;
                offer.Scope = Scope;
                offer.Provider = Provider;
                offer.OfferType = OfferType;
                offer.LinkLabel = LinkLabel;
                offer.ProductName = ProductName;
                offer.ProviderProductId = ProviderProductId;
                offer.ProviderPriceId = ProviderPriceId;
                offer.ProviderPaymentLinkId = ProviderPaymentLinkId;
                offer.ProviderPaymentUrl = ProviderPaymentUrl;
                offer.AmountMinor = AmountMinor;
                offer.Currency = Currency;
                offer.BillingInterval = BillingInterval;
                offer.BillingIntervalCount = BillingIntervalCount;
                offer.IsActive = IsActive;
                offer.Created = Created;
                offer.Updated = Updated;
            }
        }

        private sealed class PaymentSubscriptionSnapshot
        {
            private readonly int _id;
            private readonly string _provider;
            private readonly string _providerSubscriptionId;
            private readonly string _providerPriceId;
            private readonly string _providerProductId;
            private readonly string _status;
            private readonly DateTime _updated;

            private PaymentSubscriptionSnapshot(PaymentSubscription source)
            {
                _id = source.Id;
                _provider = source.Provider;
                _providerSubscriptionId = source.ProviderSubscriptionId;
                _providerPriceId = source.ProviderPriceId;
                _providerProductId = source.ProviderProductId;
                _status = source.Status;
                _updated = source.Updated;
            }

            public static PaymentSubscriptionSnapshot Capture(PaymentSubscription source) => new(source);

            public string Provider => _provider;
            public string ProviderSubscriptionId => _providerSubscriptionId;
            public string ProviderPriceId => _providerPriceId;
            public string Status => _status;

            public void Restore(ApplicationDbContext db)
            {
                var target = db.PaymentSubscriptions.Local.FirstOrDefault(item => item.Id == _id) ??
                    db.PaymentSubscriptions.Find(_id);
                if (target == null)
                {
                    return;
                }

                target.ProviderPriceId = _providerPriceId;
                target.ProviderProductId = _providerProductId;
                target.Updated = _updated;
            }
        }

        private sealed record MobilePayChargeReplacement(
            string AgreementId,
            string OriginalChargeId,
            long OriginalAmountMinor,
            string Currency,
            string OriginalDescription,
            DateTime Due,
            string ReplacementChargeId,
            int RetryDays,
            string TransactionType);

        private static string CreateRescheduledChargeId(
            string agreementId,
            string originalChargeId,
            long amountMinor,
            string description,
            DateTime due,
            string purpose)
        {
            var material = $"{agreementId}:{originalChargeId}:{amountMinor}:{description}:{due:O}:{purpose}";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)))
                .ToLowerInvariant()[..12];
            return $"gamma-{due:yyyyMMdd}-{purpose}-{hash}";
        }

        private static string GetStripePriceDescription(PaymentOffer offer)
        {
            return $"{offer.ProductName} - {offer.OfferType}";
        }

        private static bool IsMatchingStripePrice(Stripe.Price price, PaymentOffer offer, Stripe.Product product)
        {
            if (price == null || product == null ||
                !price.Active ||
                price.ProductId != product.Id ||
                price.UnitAmount != offer.AmountMinor ||
                !string.Equals(price.Currency, offer.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(offer.OfferType, PaymentOfferTypes.OneTime, StringComparison.OrdinalIgnoreCase))
            {
                return price.Type == "one_time";
            }

            return string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase) &&
                price.Type == "recurring" &&
                price.Recurring != null &&
                string.Equals(price.Recurring.Interval, ToStripeInterval(offer.BillingInterval), StringComparison.OrdinalIgnoreCase) &&
                price.Recurring.IntervalCount == offer.BillingIntervalCount.GetValueOrDefault(1);
        }

        private static string ToStripeInterval(string interval)
        {
            if (string.IsNullOrWhiteSpace(interval))
            {
                return "year";
            }

            return interval?.Trim().ToUpperInvariant() switch
            {
                "DAY" => "day",
                "WEEK" => "week",
                "MONTH" => "month",
                "YEAR" => "year",
                _ => throw new ArgumentException("Stripe-betalinger kræver et gyldigt betalingsinterval.")
            };
        }

        private static bool IsEventPaymentOpen(ContentItem item)
        {
            if (item == null ||
                !string.Equals(item.Type, ContentTypes.Event, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(item.Status, ContentStatuses.Published, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var deadline = item.EndDate ?? item.StartDate;
            return deadline.HasValue && deadline.Value > DateTime.UtcNow;
        }

        private static bool IsSupportedMobilePayInterval(string interval)
        {
            return interval == "DAY" || interval == "WEEK" || interval == "MONTH" || interval == "YEAR";
        }

        private static string NormalizeProvider(string value)
        {
            var normalized = Required(value, "Udbyder").ToUpperInvariant();
            if (normalized == "VIPPS" || normalized == PaymentOfferProviders.MobilePay)
            {
                return PaymentOfferProviders.MobilePay;
            }

            if (normalized == PaymentOfferProviders.Stripe)
            {
                return PaymentOfferProviders.Stripe;
            }

            throw new ArgumentException("Udbyder skal være Stripe eller MobilePay.");
        }

        private static long ToMinorUnits(decimal amount)
        {
            var minorUnits = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
            if (minorUnits < MinimumAmountMinor)
            {
                throw new ArgumentException("Prisen skal være mindst 10,00 kr.");
            }

            if (minorUnits > long.MaxValue)
            {
                throw new ArgumentException("Prisen er for høj.");
            }

            return (long)minorUnits;
        }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} er påkrævet.");
            }

            return value.Trim();
        }

        private string GetPaymentAttemptKey(string userId, PaymentOffer offer)
        {
            // The client keeps one key for one logical checkout flow. This makes
            // retries after a timeout idempotent, while a newly generated key
            // after a known cancellation starts a genuinely new attempt.
            var suppliedKey = Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(suppliedKey) && suppliedKey.Length <= 128)
            {
                return suppliedKey;
            }

            // Keep a safe fallback for non-browser/API clients that do not send
            // an idempotency key. The rate limiter still protects this endpoint.
            var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (15 * 60);
            return $"server:{offer.Provider}:{offer.OfferType}:{userId}:{offer.Id}:{bucket}";
        }

        private string GetStripeIdempotencyKey(string userId, PaymentOffer offer)
        {
            var value = $"stripe:{offer.OfferType}:{offer.Id}:{userId}:{GetPaymentAttemptKey(userId, offer)}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        private string PublicUrl(string path)
        {
            var configuredBaseUrl = _configuration?["PublicBaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                // Unit tests instantiate this controller directly without the host configuration.
                // The running application is required to provide PublicBaseUrl at startup.
                if (_configuration == null)
                {
                    return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{path.TrimStart('/')}";
                }

                throw new InvalidOperationException("PublicBaseUrl er ikke konfigureret.");
            }

            return $"{configuredBaseUrl}/{path.TrimStart('/')}";
        }

        private Task<string> GetUserPhoneNumberAsync(string userId, CancellationToken cancellationToken)
        {
            return _db.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.PhoneNumber)
                .SingleOrDefaultAsync(cancellationToken);
        }

        private async Task<bool> HasPaidMembershipAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            return await _db.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == userId &&
                    (user.Status == UserStatus.BETALT || user.Status == UserStatus.STUDERENDE), cancellationToken);
        }

        private Task<bool> HasOpenMembershipSubscriptionAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            return _db.PaymentSubscriptions.AnyAsync(subscription =>
                subscription.UserId == userId &&
                (subscription.Status == PaymentSubscriptionStatuses.Pending ||
                 subscription.Status == PaymentSubscriptionStatuses.Active ||
                 subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                 subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                 subscription.Status == PaymentSubscriptionStatuses.Incomplete), cancellationToken);
        }

        private async Task AbandonPendingMobilePayMembershipsAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            if (_mobilePayReconciliation == null)
            {
                return;
            }

            var pendingSubscriptions = await _db.PaymentSubscriptions
                .Where(subscription =>
                    subscription.UserId == userId &&
                    subscription.Provider == PaymentOfferProviders.MobilePay &&
                    subscription.Status == PaymentSubscriptionStatuses.Pending)
                .ToListAsync(cancellationToken);

            foreach (var subscription in pendingSubscriptions)
            {
                await _mobilePayReconciliation.TryAbandonPendingAsync(
                    subscription,
                    cancellationToken);
            }
        }

        private static bool IsPendingMobilePayAgreement(PaymentSubscription subscription)
        {
            return subscription != null &&
                string.Equals(subscription.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                subscription.Status == PaymentSubscriptionStatuses.Pending &&
                subscription.ProviderSubscriptionId?.StartsWith("pending:", StringComparison.Ordinal) == true;
        }

        private static string PendingMobilePaySubscriptionId(string reference)
        {
            var value = $"pending:{reference}";
            if (value.Length > 128)
            {
                throw new InvalidOperationException("MobilePay-referencen er for lang.");
            }

            return value;
        }

        private static string GetPendingMobilePayReference(string providerSubscriptionId)
        {
            const string prefix = "pending:";
            return providerSubscriptionId?.StartsWith(prefix, StringComparison.Ordinal) == true
                ? providerSubscriptionId[prefix.Length..]
                : null;
        }

        private static string MetadataValue(System.Collections.Generic.IDictionary<string, string> metadata, string key)
        {
            return metadata != null && metadata.TryGetValue(key, out var value) ? value : null;
        }

        private static bool IsEventParticipant(string registrationType)
        {
            return string.Equals(registrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase)
                || string.Equals(registrationType, RegistrationTypes.Organizer, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCancelledPaymentState(string state)
        {
            return string.Equals(state, "ABORTED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "EXPIRED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "CANCELLED", StringComparison.OrdinalIgnoreCase);
        }
    }
}
