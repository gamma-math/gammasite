using System;
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
}

public sealed record MobilePayPayment(
    string Reference,
    string RedirectUrl,
    string State,
    long AmountMinor,
    long CapturedAmountMinor,
    string Currency);

public sealed class MobilePayService : IMobilePayService
{
    private const string SystemName = "gamma";
    private const string SystemVersion = "1.0.0";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public MobilePayService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
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
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request.Content = JsonContent.Create(new
        {
            amount = new { currency = currency.ToUpperInvariant(), value = amountMinor },
            paymentMethod = new { type = "WALLET" },
            reference,
            userFlow = "WEB_REDIRECT",
            returnUrl,
            paymentDescription = description
        });

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

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "/accesstoken/get", null);
        request.Headers.Add("client_id", RequiredConfiguration("VIPPS_CLIENT_ID"));
        request.Headers.Add("client_secret", RequiredConfiguration("VIPPS_CLIENT_SECRET"));
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
        request.Headers.Add("Ocp-Apim-Subscription-Key", RequiredConfiguration("VIPPS_OCP_APIM_SUBSCRIPTION_KEY"));
        request.Headers.Add("Merchant-Serial-Number", RequiredConfiguration("VIPPS_MSN"));
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
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"MobilePay-konfigurationen mangler {key}.")
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
}
