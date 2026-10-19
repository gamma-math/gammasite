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
            var name = ReadString(root, "name") ?? "unknown";
            var reference = ReadString(root, "reference") ?? "unknown";
            var webhookId = Request.Headers["Webhook-Id"].FirstOrDefault() ?? "unknown";

            _logger.LogInformation(
                "Received Vipps webhook {Name} for payment {Reference} from webhook {WebhookId}.",
                name,
                reference,
                webhookId);

            if (_db != null && _mobilePayService != null && _referenceService != null)
            {
                await HandleEventPaymentAsync(name, reference, cancellationToken);
            }
        }

        return Ok();
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
}
