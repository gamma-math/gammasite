using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace GamMaSite.Services;

public interface IMobilePayEventReferenceService
{
    string Create(int registrationId, int offerId, string attemptKey = null);

    bool TryParse(string reference, out int registrationId, out int offerId);

    string CreateMembership(string userId, int offerId, string attemptKey = null);

    bool TryParseMembership(string reference, out string userId, out int offerId);
}

public sealed class MobilePayEventReferenceService : IMobilePayEventReferenceService
{
    private readonly IConfiguration _configuration;

    public MobilePayEventReferenceService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Create(int registrationId, int offerId, string attemptKey = null)
    {
        var nonce = CreateNonce($"event:{registrationId}:{offerId}:{attemptKey ?? Guid.NewGuid().ToString("N")}", 12);
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

    public string CreateMembership(string userId, int offerId, string attemptKey = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Bruger-id er påkrævet.", nameof(userId));
        }

        var encodedUserId = Encode(userId);
        var nonce = CreateNonce($"membership:{userId}:{offerId}:{attemptKey ?? Guid.NewGuid().ToString("N")}", 6);
        var payload = $"{userId}:{offerId}:{nonce}";
        return $"gm{encodedUserId}-o{offerId}-n{nonce}-{CreateSignature(payload)[..16]}";
    }

    public bool TryParseMembership(string reference, out string userId, out int offerId)
    {
        userId = null;
        offerId = 0;

        var parts = reference?.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts is null ||
            parts.Length < 4 ||
            !parts[0].StartsWith("gm", StringComparison.Ordinal) ||
            !parts[^3].StartsWith('o') ||
            !parts[^2].StartsWith('n') ||
            parts[^2].Length <= 1 ||
            !int.TryParse(parts[^3][1..], out offerId))
        {
            return false;
        }

        var encodedUserId = string.Join('-', parts.Take(parts.Length - 3))[2..];
        try
        {
            userId = Decode(encodedUserId);
        }
        catch (FormatException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var nonce = parts[^2][1..];
        var expected = CreateSignature($"{userId}:{offerId}:{nonce}")[..16];
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(parts[^1]));
    }

    private static string Encode(string value)
    {
        return Guid.TryParse(value, out var guid)
            ? guid.ToString("N")
            : value;
    }

    private static string Decode(string value)
    {
        return value.Length == 32 && Guid.TryParseExact(value, "N", out var guid)
            ? guid.ToString()
            : value;
    }

    private string CreateSignature(string payload)
    {
        var secret = _configuration["MobilePay:ReferenceSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = _configuration["MobilePay:WebhookSecret"];
        }
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("MobilePay reference secret er ikke konfigureret.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))[..24].ToLowerInvariant();
    }

    private static string CreateNonce(string value, int length)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant()[..length];
    }
}
