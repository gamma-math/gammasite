using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace GamMaSite.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/payments/vipps")]
public sealed class VippsWebhookController : ControllerBase
{
    private const string SignaturePrefix = "HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature=";

    private readonly IConfiguration _configuration;
    private readonly ILogger<VippsWebhookController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IMobilePayService _mobilePayService;
    private readonly IMobilePayEventReferenceService _referenceService;

    public VippsWebhookController(
        IConfiguration configuration,
        ILogger<VippsWebhookController> logger,
        ApplicationDbContext db,
        IMobilePayService mobilePayService,
        IMobilePayEventReferenceService referenceService)
    {
        _configuration = configuration;
        _logger = logger;
        _db = db;
        _mobilePayService = mobilePayService;
        _referenceService = referenceService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var secret = _configuration["VIPPS_WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Vipps webhook secret is not configured.");
            return Problem("Vipps webhook er ikke konfigureret.");
        }

        await using var bodyStream = new MemoryStream();
        await Request.Body.CopyToAsync(bodyStream, cancellationToken);
        var body = bodyStream.ToArray();

        if (!IsValidSignature(secret, body))
        {
            _logger.LogWarning("Vipps webhook signature validation failed.");
            return Unauthorized();
        }

        JsonDocument payload;
        try
        {
            payload = JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Vipps webhook payload was not valid JSON.");
            return BadRequest();
        }

        using (payload)
        {
            var root = payload.RootElement;
            var name = ReadString(root, "name") ?? ReadString(root, "eventType") ?? "unknown";
            var reference = ReadString(root, "reference") ?? "unknown";
            var agreementId = ReadString(root, "agreementId");
            var chargeId = ReadString(root, "chargeId");
            var webhookId = Request.Headers["Webhook-Id"].FirstOrDefault() ?? "unknown";

            _logger.LogInformation(
                "Received Vipps webhook {Name} for payment {Reference}, agreement {AgreementId}, charge {ChargeId} from webhook {WebhookId}.",
                name,
                reference,
                agreementId,
                chargeId,
                webhookId);

            if (_db != null && _mobilePayService != null && _referenceService != null)
            {
                if (name.StartsWith("recurring.", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleRecurringPaymentAsync(
                        name,
                        agreementId,
                        chargeId,
                        ReadDateTime(root, "occurred") ?? DateTime.UtcNow,
                        cancellationToken);
                }
                else
                {
                    await HandlePaymentAsync(name, reference, cancellationToken);
                }
            }
        }

        return Ok();
    }

    private async Task HandlePaymentAsync(string name, string reference, CancellationToken cancellationToken)
    {
        if (_referenceService.TryParseMembership(reference, out var userId, out var membershipOfferId))
        {
            await HandleMembershipPaymentAsync(name, reference, userId, membershipOfferId, cancellationToken);
            return;
        }

        await HandleEventPaymentAsync(name, reference, cancellationToken);
    }

    private async Task HandleMembershipPaymentAsync(
        string name,
        string reference,
        string userId,
        int offerId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(name, "AUTHORIZED", StringComparison.OrdinalIgnoreCase))
        {
            var payment = await _mobilePayService.GetPaymentAsync(reference, cancellationToken);
            if (payment.CapturedAmountMinor < payment.AmountMinor && payment.AmountMinor > 0)
            {
                await _mobilePayService.CaptureAsync(reference, payment.AmountMinor, payment.Currency ?? "DKK", cancellationToken);
            }

            return;
        }

        if (!string.Equals(name, "CAPTURED", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var offer = await _db.PaymentOffers
            .FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken);
        if (offer == null ||
            !string.Equals(offer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase) ||
            offer.ContentItemId.HasValue ||
            !string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(offer.OfferType, PaymentOfferTypes.OneTime, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Could not resolve MobilePay membership payment {Reference}.", reference);
            return;
        }

        var user = await _db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("MobilePay membership payment {Reference} could not be mapped to user {UserId}.", reference, userId);
            return;
        }

        user.MarkAsPayed();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleEventPaymentAsync(string name, string reference, CancellationToken cancellationToken)
    {
        if (!_referenceService.TryParse(reference, out var registrationId, out var offerId))
        {
            // References created by older/manual sandbox tests are intentionally ignored.
            return;
        }

        if (string.Equals(name, "AUTHORIZED", StringComparison.OrdinalIgnoreCase))
        {
            var payment = await _mobilePayService.GetPaymentAsync(reference, cancellationToken);
            if (payment.CapturedAmountMinor < payment.AmountMinor && payment.AmountMinor > 0)
            {
                await _mobilePayService.CaptureAsync(reference, payment.AmountMinor, payment.Currency ?? "DKK", cancellationToken);
            }

            return;
        }

        if (!string.Equals(name, "CAPTURED", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var registration = await _db.EventRegistrations
            .FirstOrDefaultAsync(item => item.Id == registrationId, cancellationToken);
        var offer = await _db.PaymentOffers
            .Include(item => item.ContentItem)
            .FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken);

        if (registration == null || offer?.ContentItem?.Type != ContentTypes.Event ||
            registration.ContentItemId != offer.ContentItemId ||
            !string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Could not resolve MobilePay event payment {Reference}.", reference);
            return;
        }

        if (!string.Equals(registration.RegistrationType, RegistrationTypes.Attendee, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!registration.Registered)
        {
            registration.Registered = true;
            registration.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandleRecurringPaymentAsync(
        string eventType,
        string agreementId,
        string chargeId,
        DateTime occurred,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(agreementId))
        {
            _logger.LogWarning("MobilePay recurring webhook {EventType} did not contain an agreement ID.", eventType);
            return;
        }

        var subscription = await _db.PaymentSubscriptions
            .FirstOrDefaultAsync(item => item.Provider == PaymentOfferProviders.MobilePay &&
                item.ProviderSubscriptionId == agreementId, cancellationToken);
        if (subscription == null)
        {
            _logger.LogWarning("Could not resolve MobilePay agreement {AgreementId}.", agreementId);
            return;
        }

        if (eventType.Equals("recurring.agreement-activated.v1", StringComparison.OrdinalIgnoreCase))
        {
            // Our agreements always contain an initial DIRECT_CAPTURE charge.
            // MobilePay only activates such an agreement after that initial
            // payment has succeeded, so this event is sufficient to mark the
            // membership as paid even if the charge webhook arrives later.
            var activatedOffer = await FindRecurringMembershipOfferAsync(subscription, cancellationToken);
            subscription.Status = PaymentSubscriptionStatuses.Active;
            subscription.LastPaidAt = occurred;
            subscription.CurrentPeriodStart = occurred;
            subscription.NextChargeAt = activatedOffer == null ? null : GetNextChargeDate(occurred, activatedOffer);
            subscription.Updated = DateTime.UtcNow;

            var activatedUser = await _db.Users.SingleOrDefaultAsync(item => item.Id == subscription.UserId, cancellationToken);
            if (activatedUser != null)
            {
                activatedUser.MarkAsPayed();
                if (activatedUser.KontingentDato == default || activatedUser.KontingentDato < occurred)
                {
                    activatedUser.KontingentDato = occurred;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (eventType.Equals("recurring.agreement-stopped.v1", StringComparison.OrdinalIgnoreCase))
        {
            subscription.Status = PaymentSubscriptionStatuses.Canceled;
            subscription.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (eventType.Equals("recurring.agreement-expired.v1", StringComparison.OrdinalIgnoreCase) ||
            eventType.Equals("recurring.agreement-rejected.v1", StringComparison.OrdinalIgnoreCase))
        {
            subscription.Status = PaymentSubscriptionStatuses.Expired;
            subscription.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (eventType.Equals("recurring.charge-failed.v1", StringComparison.OrdinalIgnoreCase) ||
            eventType.Equals("recurring.charge-creation-failed.v1", StringComparison.OrdinalIgnoreCase))
        {
            subscription.Status = PaymentSubscriptionStatuses.PastDue;
            subscription.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!eventType.Equals("recurring.charge-captured.v1", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(chargeId))
        {
            return;
        }

        var offer = await FindRecurringMembershipOfferAsync(subscription, cancellationToken);
        subscription.Status = PaymentSubscriptionStatuses.Active;
        subscription.LastPaidAt = occurred;
        subscription.CurrentPeriodStart = occurred;
        subscription.NextChargeAt = offer == null ? null : GetNextChargeDate(occurred, offer);
        subscription.Updated = DateTime.UtcNow;

        var user = await _db.Users.SingleOrDefaultAsync(item => item.Id == subscription.UserId, cancellationToken);
        if (user != null)
        {
            user.MarkAsPayed();
            if (user.KontingentDato == default || user.KontingentDato < occurred)
            {
                user.KontingentDato = occurred;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (offer == null || !subscription.NextChargeAt.HasValue)
        {
            _logger.LogWarning("MobilePay agreement {AgreementId} was paid, but its payment offer could not be resolved.", agreementId);
            return;
        }

        var nextChargeId = CreateNextChargeId(agreementId, subscription.NextChargeAt.Value);
        try
        {
            await _mobilePayService.CreateRecurringChargeAsync(
                agreementId,
                nextChargeId,
                offer.AmountMinor.GetValueOrDefault(),
                offer.Currency ?? "DKK",
                offer.ProductName,
                subscription.NextChargeAt.Value,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(
                exception,
                "MobilePay agreement {AgreementId} was paid, but the next charge {ChargeId} could not be created.",
                agreementId,
                nextChargeId);
        }
    }

    private async Task<PaymentOffer> FindRecurringMembershipOfferAsync(
        PaymentSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (int.TryParse(subscription.ProviderProductId, out var paymentOfferId))
        {
            var offer = await _db.PaymentOffers
                .FirstOrDefaultAsync(item => item.Id == paymentOfferId, cancellationToken);
            if (IsRecurringMembershipOffer(offer))
            {
                return offer;
            }
        }

        return await _db.PaymentOffers
            .Where(item => item.IsActive &&
                item.Scope == PaymentOfferScopes.Membership &&
                item.Provider == PaymentOfferProviders.MobilePay &&
                item.OfferType == PaymentOfferTypes.Recurring &&
                item.ProductName == subscription.ProductName)
            .OrderByDescending(item => item.Updated)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsRecurringMembershipOffer(PaymentOffer offer)
    {
        return offer != null && offer.IsActive &&
            string.Equals(offer.Scope, PaymentOfferScopes.Membership, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(offer.Provider, PaymentOfferProviders.MobilePay, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(offer.OfferType, PaymentOfferTypes.Recurring, StringComparison.OrdinalIgnoreCase);
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

    private static string CreateNextChargeId(string agreementId, DateTime due)
    {
        var safeAgreementId = new string(agreementId.Where(char.IsLetterOrDigit).ToArray());
        return $"gamma-{safeAgreementId}-{due:yyyyMMdd}";
    }

    private bool IsValidSignature(string secret, byte[] body)
    {
        var date = Request.Headers["x-ms-date"].ToString();
        var contentHash = Request.Headers["x-ms-content-sha256"].ToString();
        var authorization = Request.Headers.Authorization.ToString();
        var host = Request.Headers.Host.ToString();
        if (string.IsNullOrWhiteSpace(host))
        {
            host = Request.Host.Value;
        }

        if (string.IsNullOrWhiteSpace(date) ||
            string.IsNullOrWhiteSpace(contentHash) ||
            string.IsNullOrWhiteSpace(authorization) ||
            string.IsNullOrWhiteSpace(host) ||
            !authorization.StartsWith(SignaturePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        byte[] suppliedContentHash;
        byte[] suppliedSignature;
        try
        {
            suppliedContentHash = Convert.FromBase64String(contentHash);
            suppliedSignature = Convert.FromBase64String(authorization[SignaturePrefix.Length..]);
        }
        catch (FormatException)
        {
            return false;
        }

        var expectedContentHash = SHA256.HashData(body);
        if (!CryptographicOperations.FixedTimeEquals(suppliedContentHash, expectedContentHash))
        {
            return false;
        }

        var pathAndQuery = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
        var signedString = $"{Request.Method}\n{pathAndQuery}\n{date};{host};{contentHash}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedString));

        return CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature);
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static DateTime? ReadDateTime(JsonElement root, string propertyName)
    {
        var value = ReadString(root, propertyName);
        return DateTime.TryParse(value, out var result) ? result.ToUniversalTime() : null;
    }
}
