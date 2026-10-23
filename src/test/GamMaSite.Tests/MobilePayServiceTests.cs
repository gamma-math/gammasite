using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GamMaSite.Tests;

public sealed class MobilePayServiceTests
{
    [Fact]
    public async Task GetAgreementAsync_ReadsAgreementDates()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/accesstoken/get")
            {
                return JsonResponse("{\"access_token\":\"test-token\"}");
            }

            Assert.Equal("/recurring/v3/agreements/agr-1", request.RequestUri?.AbsolutePath);
            return JsonResponse(
                "{\"agreementId\":\"agr-1\",\"status\":\"ACTIVE\",\"chargeId\":\"charge-1\",\"created\":\"2026-10-20T09:12:00Z\",\"start\":\"2026-10-20T09:12:00Z\"}");
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://apitest.example/")
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MobilePay:Msn"] = "test-msn",
                ["MobilePay:ClientId"] = "test-client",
                ["MobilePay:ClientSecret"] = "test-secret",
                ["MobilePay:SubscriptionKey"] = "test-subscription-key"
            })
            .Build();

        var service = new MobilePayService(
            client,
            configuration,
            NullLogger<MobilePayService>.Instance);

        var agreement = await service.GetAgreementAsync("agr-1");

        Assert.Equal("agr-1", agreement.AgreementId);
        Assert.Equal("ACTIVE", agreement.Status);
        Assert.Equal(new DateTime(2026, 10, 20, 9, 12, 0, DateTimeKind.Utc), agreement.CreatedAt);
        Assert.Equal(new DateTime(2026, 10, 20, 9, 12, 0, DateTimeKind.Utc), agreement.StartAt);
    }

    [Fact]
    public async Task GetRecurringChargesAsync_ReadsPendingChargeDetails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/accesstoken/get")
            {
                return JsonResponse("{\"access_token\":\"test-token\"}");
            }

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/recurring/v3/agreements/agr-1/charges", request.RequestUri?.AbsolutePath);
            return JsonResponse(
                "[{\"id\":\"charge-1\",\"agreementId\":\"agr-1\",\"status\":\"PENDING\",\"type\":\"RECURRING\",\"amount\":{\"value\":15000,\"currency\":\"DKK\"},\"description\":\"Kontingent\",\"due\":\"2027-02-01T00:00:00Z\",\"retryDays\":3,\"transactionType\":\"RESERVE_CAPTURE\"}]");
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://apitest.example/")
        };
        var configuration = CreateConfiguration();
        var service = new MobilePayService(client, configuration, NullLogger<MobilePayService>.Instance);

        var charges = await service.GetRecurringChargesAsync("agr-1");

        var charge = Assert.Single(charges);
        Assert.Equal("charge-1", charge.Id);
        Assert.Equal("agr-1", charge.AgreementId);
        Assert.Equal("PENDING", charge.Status);
        Assert.Equal("RECURRING", charge.Type);
        Assert.Equal(15000, charge.AmountMinor);
        Assert.Equal("DKK", charge.Currency);
        Assert.Equal("Kontingent", charge.Description);
        Assert.Equal(new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc), charge.Due);
        Assert.Equal(3, charge.RetryDays);
        Assert.Equal("RESERVE_CAPTURE", charge.TransactionType);
    }

    [Fact]
    public async Task CancelRecurringChargeAsyncSendsDeleteRequest()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/accesstoken/get")
            {
                return JsonResponse("{\"access_token\":\"test-token\"}");
            }

            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/recurring/v3/agreements/agr-1/charges/charge-1", request.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://apitest.example/")
        };
        var service = new MobilePayService(client, CreateConfiguration(), NullLogger<MobilePayService>.Instance);

        await service.CancelRecurringChargeAsync("agr-1", "charge-1");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MobilePay:Msn"] = "test-msn",
                ["MobilePay:ClientId"] = "test-client",
                ["MobilePay:ClientSecret"] = "test-secret",
                ["MobilePay:SubscriptionKey"] = "test-subscription-key"
            })
            .Build();
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
