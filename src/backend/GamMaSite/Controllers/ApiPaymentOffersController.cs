using System;
using System.Linq;
using System.Security.Claims;
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
    [Route("api/payment-offers")]
    [AutoValidateAntiforgeryToken]
    public sealed class ApiPaymentOffersController : ControllerBase
    {
        private const string Currency = "dkk";
        private readonly ApplicationDbContext _db;
        private readonly IStripeService _stripeService;
        private readonly IMobilePayService _mobilePayService;
        private readonly IMobilePayEventReferenceService _mobilePayReferenceService;
        private readonly IAccessControlService _accessControl;

        public ApiPaymentOffersController(
            ApplicationDbContext db,
            IStripeService stripeService,
            IAccessControlService accessControl,
            IMobilePayService mobilePayService = null,
            IMobilePayEventReferenceService mobilePayReferenceService = null)
        {
            _db = db;
            _stripeService = stripeService;
            _mobilePayService = mobilePayService;
            _mobilePayReferenceService = mobilePayReferenceService;
            _accessControl = accessControl;
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
        [Authorize(Policy = PermissionPolicies.ContentEdit)]
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

        [HttpGet("admin/{id:int}")]
        [Authorize(Policy = PermissionPolicies.ContentEdit)]
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
        [Authorize(Policy = PermissionPolicies.ContentEdit)]
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

            try
            {
                var previousAmountMinor = offer.AmountMinor;
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

                if (request.IsActive.HasValue)
                {
                    offer.IsActive = request.IsActive.Value;
                }

                if (amountChanged &&
                    string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase))
                {
                    await UpdateMobilePayAgreementAmountsAsync(offer, offer.AmountMinor.Value, cancellationToken);
                }

                offer.Updated = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return Ok(offer.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(502, new { error = ex.Message });
            }
        }

        private async Task UpdateMobilePayAgreementAmountsAsync(
            PaymentOffer offer,
            long amountMinor,
            CancellationToken cancellationToken)
        {
            var offerId = offer.Id.ToString();
            var subscriptions = await _db.PaymentSubscriptions
                .Where(subscription =>
                    subscription.Provider == PaymentOfferProviders.MobilePay &&
                    subscription.ProviderProductId == offerId &&
                    (subscription.Status == PaymentSubscriptionStatuses.Pending ||
                     subscription.Status == PaymentSubscriptionStatuses.Active ||
                     subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                     subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                     subscription.Status == PaymentSubscriptionStatuses.Incomplete))
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
            {
                return;
            }

            if (_mobilePayService == null)
            {
                throw new InvalidOperationException("MobilePay-tjenesten er ikke konfigureret.");
            }

            foreach (var subscription in subscriptions)
            {
                await _mobilePayService.UpdateAgreementAmountAsync(
                    subscription.ProviderSubscriptionId,
                    amountMinor,
                    cancellationToken);
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .ThenInclude(item => item.Links)
                .FirstOrDefaultAsync(item => item.Id == id && item.IsActive);

            return offer == null || !IsEventOffer(offer) || !IsEventPaymentOpen(offer.ContentItem)
                ? NotFound()
                : Ok(offer.ToDto());
        }

        [HttpGet("success-context")]
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

            if (int.TryParse(paymentOfferId, out var parsedOfferId))
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

            if (offer != null &&
                string.Equals(offer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(offer.Provider, PaymentOfferProviders.Stripe, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(stripeSession.Mode, "payment", StringComparison.OrdinalIgnoreCase))
            {
                await MarkMembershipPaidAsync(userId, HttpContext.RequestAborted);
            }

            if (offer?.ContentItem?.Type != ContentTypes.Event || string.IsNullOrWhiteSpace(offer.ContentItem.Slug))
            {
                return Ok(new { type = "MEMBERSHIP" });
            }

            var registration = await _db.EventRegistrations
                .FirstOrDefaultAsync(item => item.ContentItemId == offer.ContentItemId && item.UserId == userId);
            if (registration != null &&
                string.Equals(registration.RegistrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase) &&
                !registration.Registered)
            {
                registration.Registered = true;
                registration.Updated = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Ok(new { type = "EVENT", eventSlug = offer.ContentItem.Slug });
        }

        [HttpPost("event/{contentItemId:int}")]
        [Authorize]
        public async Task<IActionResult> SaveForEvent(int contentItemId, SavePaymentOfferRequest request)
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
                var offer = item.PaymentOffers.FirstOrDefault(current =>
                    current.Provider == provider && current.OfferType == PaymentOfferTypes.OneTime);

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

                await _db.SaveChangesAsync();
                var legacyPaymentLinks = item.Links
                    .Where(link => IsLegacyPaymentLink(link, offer.Id))
                    .ToList();
                if (legacyPaymentLinks.Count > 0)
                {
                    _db.ContentLinks.RemoveRange(legacyPaymentLinks);
                    await _db.SaveChangesAsync();
                }

                return Ok(offer.ToDto(item));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(502, new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/checkout")]
        [Authorize]
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
                !string.Equals(registration.RegistrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase))
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

                var reference = _mobilePayReferenceService.Create(registration.Id, offer.Id);
                var mobilePayEventPath = $"/react/events/{Uri.EscapeDataString(offer.ContentItem.Slug)}/pay";
                var returnUrl = $"{Request.Scheme}://{Request.Host}{mobilePayEventPath}/success?reference={Uri.EscapeDataString(reference)}";
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
                catch (InvalidOperationException exception)
                {
                    return StatusCode(502, new { error = exception.Message });
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
            var eventPath = $"/react/events/{Uri.EscapeDataString(offer.ContentItem.Slug)}/pay";
            var successUrl = $"{Request.Scheme}://{Request.Host}{eventPath}/success{sessionParameter}";
            var cancelUrl = $"{Request.Scheme}://{Request.Host}{eventPath}/{offer.Id}";
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
                });

            return Ok(new { id = sessionId });
        }

        [HttpGet("mobilepay-context")]
        [Authorize]
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
                if (payment.CapturedAmountMinor >= offer.AmountMinor.GetValueOrDefault())
                {
                    await MarkRegistrationPaidAsync(registration, cancellationToken);
                    return Ok(new { type = "EVENT", status = "SUCCESS", eventSlug = offer.ContentItem.Slug, offerId });
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
            catch (InvalidOperationException exception)
            {
                return StatusCode(502, new { error = exception.Message });
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

            if (!IsEventOffer(offer) || !await _accessControl.CanEditEventAsync(User, offer.ContentItemId.Value))
            {
                return Forbid();
            }

            if (offer.Provider == PaymentOfferProviders.Stripe && !string.IsNullOrWhiteSpace(offer.ProviderPriceId))
            {
                var priceIsUsedElsewhere = await _db.PaymentOffers
                    .AnyAsync(item => item.Id != offer.Id && item.ProviderPriceId == offer.ProviderPriceId);

                if (!priceIsUsedElsewhere && !await _stripeService.DeactivatePriceAsync(offer.ProviderPriceId))
                {
                    return StatusCode(502, "Stripe-prisen kunne ikke deaktiveres. Betalingstilbuddet er ikke slettet.");
                }
            }

            var paymentLinks = offer.ContentItem?.Links?
                .Where(link => string.Equals(link.Type, "PAYMENT", StringComparison.OrdinalIgnoreCase)
                    && IsLegacyPaymentLink(link, offer.Id))
                .ToList() ?? new();

            _db.ContentLinks.RemoveRange(paymentLinks);
            _db.PaymentOffers.Remove(offer);
            await _db.SaveChangesAsync();

            return NoContent();
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

            var member = await _db.Users.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            var hasPaidMembership = member?.Status == UserStatus.BETALT || member?.Status == UserStatus.STUDERENDE;
            var hasActiveSubscription = await _db.PaymentSubscriptions.AnyAsync(subscription =>
                subscription.UserId == userId &&
                (subscription.Status == PaymentSubscriptionStatuses.Active ||
                 subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                 subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                 subscription.Status == PaymentSubscriptionStatuses.Incomplete), cancellationToken);
            if (hasPaidMembership || hasActiveSubscription)
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

                await EnsureStripeOfferReferencesAsync(offer, "Betaling for medlemskab.");
                await _db.SaveChangesAsync(cancellationToken);

                var product = await _stripeService.GetProductAsync(offer.ProviderProductId);
                var price = await _stripeService.GetPriceByIdAsync(offer.ProviderPriceId);
                if (!IsMatchingStripePrice(price, offer, product))
                {
                    return Conflict("Betalingstilbuddet stemmer ikke længere overens med Stripe.");
                }

                var sessionParameter = "?session={CHECKOUT_SESSION_ID}";
                var successUrl = $"{Request.Scheme}://{Request.Host}/react/pay/success{sessionParameter}";
                var cancelUrl = $"{Request.Scheme}://{Request.Host}/react/pay/cancel";
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
                    });

                return Ok(new { provider = PaymentOfferProviders.Stripe, id = sessionId });
            }
            catch (InvalidOperationException exception)
            {
                return StatusCode(502, new { error = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (Stripe.StripeException exception)
            {
                return StatusCode(502, new { error = $"Stripe kunne ikke starte betalingen: {exception.Message}" });
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

            var reference = _mobilePayReferenceService.CreateMembership(userId, offer.Id);
            var returnUrl = $"{Request.Scheme}://{Request.Host}/react/pay/success?reference={Uri.EscapeDataString(reference)}";
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
            catch (InvalidOperationException exception)
            {
                return StatusCode(502, new { error = exception.Message });
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

            var hasPaidMembership = member?.Status == UserStatus.BETALT || member?.Status == UserStatus.STUDERENDE;
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
                await RefreshMobilePaySubscriptionAsync(openSubscription, cancellationToken))
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            var hasOpenSubscription = await _db.PaymentSubscriptions.AnyAsync(subscription =>
                subscription.UserId == userId &&
                (subscription.Status == PaymentSubscriptionStatuses.Pending ||
                 subscription.Status == PaymentSubscriptionStatuses.Active ||
                 subscription.Status == PaymentSubscriptionStatuses.Trialing ||
                 subscription.Status == PaymentSubscriptionStatuses.PastDue ||
                 subscription.Status == PaymentSubscriptionStatuses.Incomplete), cancellationToken);
            if (hasPaidMembership || hasOpenSubscription)
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

            var merchantAgreementUrl = $"{Request.Scheme}://{Request.Host}/react/account/manage/subscriptions";
            try
            {
                var reference = _mobilePayReferenceService.CreateMembership(userId, offer.Id);
                var merchantRedirectUrl =
                    $"{Request.Scheme}://{Request.Host}/react/pay/success?reference={Uri.EscapeDataString(reference)}";
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

                var now = DateTime.UtcNow;
                var subscription = await _db.PaymentSubscriptions
                    .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
                if (subscription == null)
                {
                    subscription = new PaymentSubscription
                    {
                        UserId = userId,
                        Created = now
                    };
                    _db.PaymentSubscriptions.Add(subscription);
                }

                // PaymentSubscriptions stores the current subscription only. Reuse a
                // closed record when a member starts a new agreement; UserId is unique.
                subscription.Provider = PaymentOfferProviders.MobilePay;
                subscription.ProviderSubscriptionId = agreement.AgreementId;
                subscription.ProductName = offer.ProductName;
                subscription.ProviderProductId = offer.Id.ToString();
                subscription.Status = PaymentSubscriptionStatuses.Pending;
                subscription.CurrentPeriodStart = null;
                subscription.CurrentPeriodEnd = null;
                subscription.NextChargeAt = null;
                subscription.LastPaidAt = null;
                subscription.CancelAtPeriodEnd = false;
                subscription.Updated = now;
                await _db.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    provider = PaymentOfferProviders.MobilePay,
                    url = agreement.RedirectUrl
                });
            }
            catch (InvalidOperationException exception)
            {
                return StatusCode(502, new { error = exception.Message });
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
                var payment = await _mobilePayService.GetPaymentAsync(reference, cancellationToken);
                if (payment.CapturedAmountMinor >= offer.AmountMinor.GetValueOrDefault())
                {
                    await MarkMembershipPaidAsync(userId, cancellationToken);
                    return Ok(new { type = "MEMBERSHIP", status = "SUCCESS" });
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
            catch (InvalidOperationException exception)
            {
                return StatusCode(502, new { error = exception.Message });
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

            try
            {
                var agreement = await _mobilePayService.GetAgreementAsync(
                    subscription.ProviderSubscriptionId,
                    cancellationToken);
                var status = agreement.Status?.ToUpperInvariant();
                if (status == "ACTIVE")
                {
                    subscription.Status = PaymentSubscriptionStatuses.Active;
                    subscription.Updated = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    return Ok(new { type = "MEMBERSHIP", status = "SUCCESS" });
                }

                if (status == "STOPPED" || status == "EXPIRED" || status == "REJECTED")
                {
                    subscription.Status = status == "EXPIRED"
                        ? PaymentSubscriptionStatuses.Expired
                        : PaymentSubscriptionStatuses.Canceled;
                    subscription.Updated = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    return Ok(new { type = "MEMBERSHIP", status = "CANCELLED" });
                }

                return Ok(new { type = "MEMBERSHIP", status = "PENDING" });
            }
            catch (InvalidOperationException exception)
            {
                return StatusCode(502, new { error = exception.Message });
            }
        }

        private async Task EnsureStripeReferencesAsync(PaymentOffer offer, string eventTitle, string productName, long amountMinor)
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
            if (price == null || price.ProductId != product.Id || price.UnitAmount != amountMinor ||
                price.Type != "one_time" || !string.Equals(price.Currency, Currency, StringComparison.OrdinalIgnoreCase))
            {
                price = await _stripeService.CreateOneTimePriceAsync(product.Id, amountMinor, Currency);
            }

            if (price == null)
            {
                throw new InvalidOperationException("Stripe-prisen kunne ikke oprettes.");
            }

            offer.ProviderProductId = product.Id;
            offer.ProviderPriceId = price.Id;
        }

        private async Task EnsureStripeOfferReferencesAsync(PaymentOffer offer, string description)
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
                product = await _stripeService.CreateProductAsync(offer.ProductName, description);
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

            if (!IsMatchingStripePrice(price, offer, product))
            {
                price = string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase)
                    ? await _stripeService.CreateRecurringPriceAsync(
                        product.Id,
                        offer.AmountMinor.GetValueOrDefault(),
                        Currency,
                        ToStripeInterval(offer.BillingInterval),
                        offer.BillingIntervalCount.GetValueOrDefault(1))
                    : await _stripeService.CreateOneTimePriceAsync(
                        product.Id,
                        offer.AmountMinor.GetValueOrDefault(),
                        Currency);
            }

            if (price == null)
            {
                throw new InvalidOperationException("Stripe-prisen kunne ikke oprettes.");
            }

            offer.ProviderProductId = product.Id;
            offer.ProviderPriceId = price.Id;
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

        private async Task<bool> RefreshMobilePaySubscriptionAsync(
            PaymentSubscription subscription,
            CancellationToken cancellationToken)
        {
            if (_mobilePayService == null)
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

            if (status == null || string.Equals(subscription.Status, status, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            subscription.Status = status;
            subscription.Updated = DateTime.UtcNow;
            return true;
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
            if (amount <= 0)
            {
                throw new ArgumentException("Prisen skal være større end 0.");
            }

            var minorUnits = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
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

        private static string MetadataValue(System.Collections.Generic.IDictionary<string, string> metadata, string key)
        {
            return metadata != null && metadata.TryGetValue(key, out var value) ? value : null;
        }

        private async Task MarkRegistrationPaidAsync(EventRegistration registration, CancellationToken cancellationToken)
        {
            if (string.Equals(registration.RegistrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase) &&
                !registration.Registered)
            {
                registration.Registered = true;
                registration.Updated = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task MarkMembershipPaidAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await _db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user == null)
            {
                return;
            }

            user.MarkAsPayed();
            await _db.SaveChangesAsync(cancellationToken);
        }

        private static bool IsCancelledPaymentState(string state)
        {
            return string.Equals(state, "ABORTED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "EXPIRED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "CANCELLED", StringComparison.OrdinalIgnoreCase);
        }
    }
}
