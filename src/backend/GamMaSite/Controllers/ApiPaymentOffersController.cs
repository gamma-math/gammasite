using System;
using System.Linq;
using System.Security.Claims;
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
        private const string MobilePayPaymentUrl = "https://mobilepay.dk/erhverv/betalingslink/betalingslink-svar?phone=22766&amount=150&comment=Medlemskab";

        private readonly ApplicationDbContext _db;
        private readonly IStripeService _stripeService;
        private readonly IAccessControlService _accessControl;

        public ApiPaymentOffersController(ApplicationDbContext db, IStripeService stripeService, IAccessControlService accessControl)
        {
            _db = db;
            _stripeService = stripeService;
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
                .OrderBy(offer => offer.Provider)
                .ToListAsync();

            return Ok(offers.Select(offer => offer.ToDto()));
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .ThenInclude(item => item.Links)
                .FirstOrDefaultAsync(item => item.Id == id && item.IsActive);

            return offer == null || !IsEventPaymentOpen(offer.ContentItem)
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
                        Provider = provider,
                        OfferType = PaymentOfferTypes.OneTime,
                        Created = DateTime.UtcNow
                    };
                    _db.PaymentOffers.Add(offer);
                }

                offer.ProductName = productName;
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
                    offer.ProviderPaymentUrl = MobilePayPaymentUrl;
                }

                await _db.SaveChangesAsync();

                var paymentUrl = $"/react/events/{Uri.EscapeDataString(item.Slug)}/pay/{offer.Id}";
                var link = item.Links.FirstOrDefault(current =>
                    string.Equals(current.Type, "PAYMENT", StringComparison.OrdinalIgnoreCase) &&
                    current.Url.EndsWith($"/pay/{offer.Id}", StringComparison.OrdinalIgnoreCase));

                if (link == null)
                {
                    item.Links.Add(new ContentLink
                    {
                        ContentItemId = item.Id,
                        Label = linkLabel,
                        Url = paymentUrl,
                        Type = "PAYMENT",
                        SortOrder = item.Links.Count,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow
                    });
                }
                else
                {
                    link.Label = linkLabel;
                    link.Url = paymentUrl;
                    link.Updated = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await _db.Entry(offer).Reference(current => current.ContentItem).LoadAsync();
                await _db.Entry(offer.ContentItem).Collection(current => current.Links).LoadAsync();
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

        [HttpPost("{id:int}/checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var offer = await _db.PaymentOffers
                .Include(item => item.ContentItem)
                .FirstOrDefaultAsync(item => item.Id == id && item.IsActive);

            if (offer == null || !IsEventPaymentOpen(offer.ContentItem))
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

            if (offer.Provider != PaymentOfferProviders.Stripe ||
                offer.OfferType != PaymentOfferTypes.OneTime ||
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

            if (!await _accessControl.CanEditEventAsync(User, offer.ContentItemId))
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

            var paymentUrl = $"/pay/{offer.Id}";
            var paymentLinks = offer.ContentItem?.Links?
                .Where(link => string.Equals(link.Type, "PAYMENT", StringComparison.OrdinalIgnoreCase)
                    && link.Url?.EndsWith(paymentUrl, StringComparison.OrdinalIgnoreCase) == true)
                .ToList() ?? new();

            _db.ContentLinks.RemoveRange(paymentLinks);
            _db.PaymentOffers.Remove(offer);
            await _db.SaveChangesAsync();

            return NoContent();
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
    }
}
