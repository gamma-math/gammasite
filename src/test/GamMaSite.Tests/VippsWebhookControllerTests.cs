using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

public sealed class VippsWebhookControllerTests
{
    private const string WebhookSecret = "test-vipps-webhook-secret";

    [Fact]
    public async Task ReceiveAcceptsValidSignature()
    {
        const string payload = "{\"name\":\"AUTHORIZED\",\"reference\":\"gamma-test-12345678\"}";
        var controller = CreateController(payload);

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ReceiveAcceptsCapturedPaymentPayload()
    {
        const string payload = "{\"msn\":\"2070420\",\"reference\":\"gamma-test-12345678\",\"pspReference\":\"psp-123\",\"name\":\"CAPTURED\",\"amount\":{\"currency\":\"DKK\",\"value\":100},\"success\":true}";
        var controller = CreateController(payload);
        controller.Request.Headers["Webhook-Id"] = "webhook-123";

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ReceiveMarksMembershipUserAsPaidForCapturedPayment()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        db.Users.Add(new SiteUser { Id = "user-member-test", UserName = "member@gamma.test", Status = UserStatus.OPRETTET });
        db.PaymentOffers.Add(new PaymentOffer
        {
            Id = 7,
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.OneTime,
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VIPPS_WEBHOOK_SECRET"] = WebhookSecret,
                ["VIPPS_REFERENCE_SECRET"] = WebhookSecret
            })
            .Build();
        var referenceService = new MobilePayEventReferenceService(configuration);
        var reference = referenceService.CreateMembership("user-member-test", 7);
        var payload = $"{{\"reference\":\"{reference}\",\"name\":\"CAPTURED\"}}";
        var controller = CreateController(
            payload,
            db: db,
            mobilePayService: new Mock<IMobilePayService>().Object,
            referenceService: referenceService);

        var result = await controller.Receive(default);

        Assert.IsType<OkResult>(result);
        var user = await db.Users.SingleAsync(item => item.Id == "user-member-test");
        Assert.Equal(UserStatus.BETALT, user.Status);
    }

    [Fact]
    public async Task ReceiveRejectsInvalidSignature()
    {
        const string payload = "{\"name\":\"AUTHORIZED\",\"reference\":\"gamma-test-12345678\"}";
        var controller = CreateController(payload);
        controller.Request.Headers["Authorization"] = "HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature=invalid";

        var result = await controller.Receive(default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ReceiveRejectsBodyThatWasChangedAfterSigning()
    {
        const string signedPayload = "{\"name\":\"AUTHORIZED\",\"reference\":\"gamma-test-12345678\"}";
        var controller = CreateController(signedPayload);
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            "{\"name\":\"CAPTURED\",\"reference\":\"gamma-test-12345678\"}"));

        var result = await controller.Receive(default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ReceiveReturnsBadRequestForInvalidJsonWithValidSignature()
    {
        var controller = CreateController("not-json");

        var result = await controller.Receive(default);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task ReceiveReturnsProblemWhenSecretIsMissing()
    {
        const string payload = "{\"name\":\"AUTHORIZED\",\"reference\":\"gamma-test-12345678\"}";
        var controller = CreateController(payload, webhookSecret: null);

        var result = await controller.Receive(default);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, problem.StatusCode);
    }

    private static VippsWebhookController CreateController(
        string payload,
        string? webhookSecret = WebhookSecret,
        ApplicationDbContext db = null,
        IMobilePayService mobilePayService = null,
        IMobilePayEventReferenceService referenceService = null)
    {
        var configurationValues = new Dictionary<string, string?>();
        if (webhookSecret != null)
        {
            configurationValues["VIPPS_WEBHOOK_SECRET"] = webhookSecret;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var controller = new VippsWebhookController(
            configuration,
            NullLogger<VippsWebhookController>.Instance,
            db,
            mobilePayService,
            referenceService);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/payments/vipps/webhook";
        httpContext.Request.Host = new HostString("example.test");
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var date = "Thu, 15 Oct 2026 12:00:00 GMT";
        var contentHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var signedString = $"POST\n/api/payments/vipps/webhook\n{date};example.test;{contentHash}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedString)));

        httpContext.Request.Headers["x-ms-date"] = date;
        httpContext.Request.Headers["x-ms-content-sha256"] = contentHash;
        httpContext.Request.Headers["Authorization"] = $"HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature={signature}";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }
}
