using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GamMaSite.Services;

public interface IMobilePayService
{
    Task<MobilePayPayment> CreateOneTimePaymentAsync(
        string reference,
        long amountMinor,
        string currency,
        string description,
        string returnUrl,
        CancellationToken cancellationToken = default);

    Task<MobilePayPayment> GetPaymentAsync(string reference, CancellationToken cancellationToken = default);

    Task CaptureAsync(string reference, long amountMinor, string currency, CancellationToken cancellationToken = default);

    Task<MobilePayAgreement> CreateRecurringAgreementAsync(
        string externalId,
        string phoneNumber,
        long amountMinor,
        string currency,
        string billingInterval,
        int billingIntervalCount,
        string productName,
        string productDescription,
        string merchantRedirectUrl,
        string merchantAgreementUrl,
        CancellationToken cancellationToken = default);

    Task<MobilePayAgreement> GetAgreementAsync(string agreementId, CancellationToken cancellationToken = default);

    Task UpdateAgreementAmountAsync(
        string agreementId,
        long amountMinor,
        CancellationToken cancellationToken = default);

    Task StopAgreementAsync(string agreementId, CancellationToken cancellationToken = default);

    Task CreateRecurringChargeAsync(
        string agreementId,
        string orderId,
        long amountMinor,
        string currency,
        string description,
        DateTime due,
        CancellationToken cancellationToken = default);
}

public sealed record MobilePayPayment(
    string Reference,
    string RedirectUrl,
    string State,
    long AmountMinor,
    long CapturedAmountMinor,
    string Currency);

public sealed record MobilePayAgreement(
    string AgreementId,
    string RedirectUrl,
    string Status,
    string InitialChargeId,
    DateTime? CreatedAt = null,
    DateTime? StartAt = null);

public sealed class MobilePayService : IMobilePayService
{
    private const string SystemName = "gamma";
    private const string SystemVersion = "1.0.0";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MobilePayService> _logger;

    public MobilePayService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MobilePayService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MobilePayPayment> CreateOneTimePaymentAsync(
        string reference,
        long amountMinor,
        string currency,
        string description,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = CreateRequest(HttpMethod.Post, "/epayment/v1/payments", token);
        request.Headers.Add("Idempotency-Key", CreateStableIdempotencyKey(reference));
        var payload = new Dictionary<string, object>
        {
            ["amount"] = new { currency = currency.ToUpperInvariant(), value = amountMinor },
            ["paymentMethod"] = new { type = "WALLET" },
            ["reference"] = reference,
            ["userFlow"] = "WEB_REDIRECT",
            ["returnUrl"] = returnUrl,
            ["paymentDescription"] = description
        };

        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "oprette MobilePay-betaling");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var redirectUrl = ReadString(document.RootElement, "redirectUrl");
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            throw new InvalidOperationException("MobilePay returnerede ingen redirect-url.");
        }

        return new MobilePayPayment(reference, redirectUrl, null, amountMinor, 0, currency);
    }

    public async Task<MobilePayPayment> GetPaymentAsync(string reference, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = CreateRequest(HttpMethod.Get, $"/epayment/v1/payments/{Uri.EscapeDataString(reference)}", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "hente MobilePay-betaling");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return ParsePayment(document.RootElement, reference);
    }

    public async Task CaptureAsync(string reference, long amountMinor, string currency, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = CreateRequest(HttpMethod.Post, $"/epayment/v1/payments/{Uri.EscapeDataString(reference)}/capture", token);
        request.Headers.Add("Idempotency-Key", CreateStableIdempotencyKey(reference));
        request.Content = JsonContent.Create(new
        {
            modificationAmount = new { currency = currency.ToUpperInvariant(), value = amountMinor }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "capture MobilePay-betaling");
    }

    public async Task<MobilePayAgreement> CreateRecurringAgreementAsync(
        string externalId,
        string phoneNumber,
        long amountMinor,
        string currency,
        string billingInterval,
        int billingIntervalCount,
        string productName,
        string productDescription,
        string merchantRedirectUrl,
        string merchantAgreementUrl,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = CreateRequest(HttpMethod.Post, "/recurring/v3/agreements", token);
        request.Headers.Add("Idempotency-Key", CreateStableIdempotencyKey(externalId));
        request.Content = JsonContent.Create(new
        {
            pricing = new
            {
                type = "LEGACY",
                amount = amountMinor,
                currency = currency.ToUpperInvariant()
            },
            interval = new
            {
                unit = billingInterval.ToUpperInvariant(),
                count = billingIntervalCount
            },
            initialCharge = new
            {
                amount = amountMinor,
                currency = currency.ToUpperInvariant(),
                description = productName,
                transactionType = "DIRECT_CAPTURE"
            },
            merchantRedirectUrl,
            merchantAgreementUrl,
            phoneNumber,
            productName,
            productDescription,
            externalId
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "oprette MobilePay-abonnement");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        var agreementId = ReadString(root, "agreementId") ?? ReadString(root, "id");
        var redirectUrl = ReadString(root, "vippsConfirmationUrl");
        if (string.IsNullOrWhiteSpace(agreementId) || string.IsNullOrWhiteSpace(redirectUrl))
        {
            throw new InvalidOperationException("MobilePay returnerede ikke en gyldig abonnementsaftale.");
        }

        return new MobilePayAgreement(
            agreementId,
            redirectUrl,
            ReadString(root, "status"),
            ReadString(root, "chargeId"),
            ReadDateTime(root, "created"),
            ReadDateTime(root, "start"));
    }

    public async Task<MobilePayAgreement> GetAgreementAsync(string agreementId, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = CreateRequest(
            HttpMethod.Get,
            $"/recurring/v3/agreements/{Uri.EscapeDataString(agreementId)}",
            token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "hente MobilePay-abonnement");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        return new MobilePayAgreement(
            ReadString(root, "agreementId") ?? ReadString(root, "id") ?? agreementId,
            ReadString(root, "vippsConfirmationUrl"),
            ReadString(root, "status"),
            ReadString(root, "chargeId"),
            ReadDateTime(root, "created"),
            ReadDateTime(root, "start"));
    }

    public async Task UpdateAgreementAmountAsync(
        string agreementId,
        long amountMinor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agreementId))
        {
            throw new ArgumentException("MobilePay-aftalen mangler et ID.", nameof(agreementId));
        }

        if (amountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "MobilePay-beløbet skal være større end 0.");
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/recurring/v3/agreements/{Uri.EscapeDataString(agreementId)}",
            token);
        request.Headers.Add("Idempotency-Key", CreateStableIdempotencyKey($"{agreementId}:amount:{amountMinor}"));
        request.Content = JsonContent.Create(new
        {
            pricing = new
            {
                amount = amountMinor
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "opdatere beløbet på MobilePay-aftalen");
    }

    public async Task StopAgreementAsync(string agreementId, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/recurring/v3/agreements/{Uri.EscapeDataString(agreementId)}",
            token);
        request.Headers.Add("Idempotency-Key", CreateStableIdempotencyKey(agreementId + ":stop"));
        request.Content = JsonContent.Create(new { status = "STOPPED" });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "stoppe MobilePay-abonnement");
    }

    public async Task CreateRecurringChargeAsync(
        string agreementId,
        string orderId,
        long amountMinor,
        string currency,
        string description,
        DateTime due,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var idempotencyKey = CreateStableIdempotencyKey(agreementId + ":" + orderId);
        var dueDate = due.ToString("yyyy-MM-dd");

        _logger.LogInformation(
            "Creating MobilePay recurring charge for agreement {AgreementId}. OrderId {OrderId}, Due {DueDate}, AmountMinor {AmountMinor}, Currency {Currency}, IdempotencyKey {IdempotencyKey}.",
            agreementId,
            orderId,
            dueDate,
            amountMinor,
            currency,
            idempotencyKey);

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/recurring/v3/agreements/{Uri.EscapeDataString(agreementId)}/charges",
            token);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            amount = amountMinor,
            transactionType = "DIRECT_CAPTURE",
            type = "RECURRING",
            description,
            due = dueDate,
            retryDays = 2,
            orderId
        });

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(
                exception,
                "MobilePay recurring charge request failed before receiving a response. AgreementId {AgreementId}, OrderId {OrderId}, Due {DueDate}.",
                agreementId,
                orderId,
                dueDate);
            throw;
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation(
                "MobilePay recurring charge response received. AgreementId {AgreementId}, OrderId {OrderId}, StatusCode {StatusCode}, ResponseBody {ResponseBody}.",
                agreementId,
                orderId,
                (int)response.StatusCode,
                TruncateForLog(responseBody));

            await EnsureSuccessAsync(response, "oprette næste MobilePay-opkrævning");
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "/accesstoken/get", null);
        request.Headers.Add("client_id", RequiredConfiguration("ClientId"));
        request.Headers.Add("client_secret", RequiredConfiguration("ClientSecret"));
        request.Content = JsonContent.Create(new { });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "hente MobilePay access token");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var token = ReadString(document.RootElement, "access_token");
        return string.IsNullOrWhiteSpace(token)
            ? throw new InvalidOperationException("MobilePay returnerede ingen access token.")
            : token;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Ocp-Apim-Subscription-Key", RequiredConfiguration("SubscriptionKey"));
        request.Headers.Add("Merchant-Serial-Number", RequiredConfiguration("Msn"));
        request.Headers.Add("Vipps-System-Name", SystemName);
        request.Headers.Add("Vipps-System-Version", SystemVersion);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private string RequiredConfiguration(string key)
    {
        var value = _configuration[$"MobilePay:{key}"];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"MobilePay-konfigurationen mangler MobilePay:{key}.")
            : value;
    }

    private static MobilePayPayment ParsePayment(JsonElement root, string fallbackReference)
    {
        var reference = ReadString(root, "reference") ?? fallbackReference;
        var state = ReadState(root);
        var amount = ReadAmount(root, "amount");
        if (amount.Value <= 0)
        {
            amount = ReadAmount(root, "aggregate", "authorizedAmount");
        }
        var captured = ReadAmount(root, "aggregate", "capturedAmount");
        var currency = ReadString(root, "amount", "currency") ?? captured.Currency;
        return new MobilePayPayment(reference, ReadString(root, "redirectUrl"), state, amount.Value, captured.Value, currency);
    }

    private static (long Value, string Currency) ReadAmount(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var segment in path)
        {
            if (!current.TryGetProperty(segment, out current))
            {
                return (0, null);
            }
        }

        if (current.ValueKind != JsonValueKind.Object ||
            !current.TryGetProperty("value", out var value) ||
            !value.TryGetInt64(out var amount))
        {
            return (0, null);
        }

        return (amount, current.TryGetProperty("currency", out var currency) ? currency.GetString() : null);
    }

    private static string ReadString(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var segment in path)
        {
            if (!current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static DateTime? ReadDateTime(JsonElement root, params string[] path)
    {
        var value = ReadString(root, path);
        return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)
            ? parsed.UtcDateTime
            : null;
    }

    private static string ReadState(JsonElement root)
    {
        if (!root.TryGetProperty("state", out var state))
        {
            return null;
        }

        if (state.ValueKind == JsonValueKind.String)
        {
            return state.GetString();
        }

        return state.ValueKind == JsonValueKind.Array && state.GetArrayLength() > 0
            ? state.EnumerateArray().LastOrDefault().GetString()
            : null;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Kunne ikke {operation} hos MobilePay ({(int)response.StatusCode}). {body}");
    }

    private static string CreateStableIdempotencyKey(string reference)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(reference + ":capture"));
        return new Guid(hash.Take(16).ToArray()).ToString();
    }

    private static string TruncateForLog(string value)
    {
        const int maxLength = 2000;
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "…";
    }
}
