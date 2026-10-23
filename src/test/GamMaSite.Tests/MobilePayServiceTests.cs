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
