using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

    private static VippsWebhookController CreateController(string payload, string? webhookSecret = WebhookSecret)
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
            null,
            null,
            null);

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
