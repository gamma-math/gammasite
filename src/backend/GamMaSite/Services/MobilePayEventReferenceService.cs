using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace GamMaSite.Services;

public interface IMobilePayEventReferenceService
{
    string Create(int registrationId, int offerId);

    bool TryParse(string reference, out int registrationId, out int offerId);
}

public sealed class MobilePayEventReferenceService : IMobilePayEventReferenceService
{
    private readonly IConfiguration _configuration;

    public MobilePayEventReferenceService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Create(int registrationId, int offerId)
    {
        var nonce = Guid.NewGuid().ToString("N")[..12];
        var payload = $"{registrationId}:{offerId}:{nonce}";
        return $"gamma-e{registrationId}-o{offerId}-n{nonce}-{CreateSignature(payload)}";
    }

    public bool TryParse(string reference, out int registrationId, out int offerId)
    {
        registrationId = 0;
        offerId = 0;
        var parts = reference?.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not { Length: 4 } and not { Length: 5 } ||
            !parts[0].Equals("gamma", StringComparison.Ordinal) ||
            !parts[1].StartsWith('e') ||
            !parts[2].StartsWith('o') ||
            !int.TryParse(parts[1][1..], out registrationId) ||
            !int.TryParse(parts[2][1..], out offerId))
        {
            return false;
        }

        var hasNonce = parts.Length == 5 && parts[3].StartsWith('n') && parts[3].Length > 1;
        var nonce = hasNonce ? parts[3][1..] : null;
        var payload = nonce == null
            ? $"{registrationId}:{offerId}"
            : $"{registrationId}:{offerId}:{nonce}";
        var expected = CreateSignature(payload);
        var suppliedSignature = parts[^1];
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(suppliedSignature));
    }

    private string CreateSignature(string payload)
    {
        var secret = _configuration["VIPPS_REFERENCE_SECRET"] ?? _configuration["VIPPS_WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("MobilePay reference secret er ikke konfigureret.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))[..24].ToLowerInvariant();
    }
}
