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

        [HttpPut("admin/{id:int}")]
        [Authorize(Policy = PermissionPolicies.ContentEdit)]
        public async Task<IActionResult> UpdateForAdmin(int id, UpdatePaymentOfferRequest request)
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
                offer.LinkLabel = Required(request?.LinkLabel, "Link label");
                offer.ProductName = Required(request?.ProductName, "Produktnavn");

                if (request.Amount.HasValue)
                {
                    offer.AmountMinor = ToMinorUnits(request.Amount.Value);
                }

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

                offer.Updated = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(offer.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
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

            if (offer == null || !IsEventOffer(offer) || !IsEventPaymentOpen(offer.ContentItem))
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
            if (!_mobilePayReferenceService.TryParse(reference, out var registrationId, out var offerId))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var registration = await _db.EventRegistrations
                .Include(item => item.ContentItem)
                .FirstOrDefaultAsync(item => item.Id == registrationId && item.UserId == userId, cancellationToken);
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
    }
}
