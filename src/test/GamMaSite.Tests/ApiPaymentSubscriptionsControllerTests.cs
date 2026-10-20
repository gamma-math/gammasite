using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stripe;
using Xunit;

namespace GamMaSite.Tests;

public sealed class ApiPaymentSubscriptionsControllerTests
{
    [Fact]
    public async Task Get_RejectsAnonymousUser()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await controller.Get(default);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsOnlySubscriptionsForCurrentUser()
    {
        await using var db = CreateDb();
        db.PaymentSubscriptions.AddRange(
            Subscription("user-1", "stripe-sub-1", "Kontingent"),
            Subscription("user-2", "stripe-sub-2", "Other user's subscription"));
        await db.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user);

        var result = await controller.Get(default);
        var ok = Assert.IsType<OkObjectResult>(result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var subscriptions = json.RootElement.EnumerateArray().ToList();

        var subscription = Assert.Single(subscriptions);
        Assert.Equal("stripe", subscription.GetProperty("provider").GetString());
        Assert.Equal("Kontingent", subscription.GetProperty("productName").GetString());
    }

    [Fact]
    public async Task Get_ReconcilesCanceledMobilePayAgreement()
    {
        await using var db = CreateDb();
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "agr-pending",
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Pending,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.GetAgreementAsync("agr-pending", It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new MobilePayAgreement("agr-pending", null, "STOPPED", null));
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, new Mock<IStripeService>().Object, mobilePay.Object);

        var result = await controller.Get(default);

        var ok = Assert.IsType<OkObjectResult>(result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Equal("canceled", json.RootElement[0].GetProperty("status").GetString());
        Assert.Equal(PaymentSubscriptionStatuses.Canceled, (await db.PaymentSubscriptions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Get_PopulatesMobilePayPaymentDatesFromActiveAgreement()
    {
        await using var db = CreateDb();
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            AmountMinor = 12500,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var start = new DateTime(2026, 10, 19, 8, 34, 0, DateTimeKind.Utc);
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            PaymentOfferId = offer.Id,
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "agr-active",
            ProviderProductId = offer.Id.ToString(),
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Pending,
            Created = start,
            Updated = start
        });
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.GetAgreementAsync("agr-active", It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new MobilePayAgreement("agr-active", null, "ACTIVE", "charge-1", start, start));
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, new Mock<IStripeService>().Object, mobilePay.Object);

        var response = Assert.IsType<OkObjectResult>(await controller.Get(default));

        var subscription = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal(offer.Id, subscription.PaymentOfferId);
        Assert.Equal(PaymentSubscriptionStatuses.Active, subscription.Status);
        Assert.Equal(start, subscription.LastPaidAt);
        Assert.Equal(start, subscription.CurrentPeriodStart);
        Assert.Equal(start.AddYears(1), subscription.NextChargeAt);

        using var responseJson = JsonDocument.Parse(JsonSerializer.Serialize(response.Value));
        Assert.Equal(12500, responseJson.RootElement[0].GetProperty("amountMinor").GetInt64());
        Assert.Equal("DKK", responseJson.RootElement[0].GetProperty("currency").GetString());
    }

    [Fact]
    public async Task Delete_CancelsCurrentUsersStripeSubscription()
    {
        await using var db = CreateDb();
        var paymentSubscription = Subscription("user-1", "sub-1", "Kontingent");
        paymentSubscription.Provider = "Stripe";
        db.PaymentSubscriptions.Add(paymentSubscription);
        await db.SaveChangesAsync();

        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.CancelSubscriptionAsync("sub-1"))
            .ReturnsAsync(new Subscription { Id = "sub-1", Status = "canceled" });
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, stripe.Object);

        var result = await controller.Delete("sub-1", default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("canceled", ok.Value?.GetType().GetProperty("status")?.GetValue(ok.Value));
        Assert.Equal("canceled", (await db.PaymentSubscriptions.SingleAsync()).Status);
        stripe.Verify(value => value.CancelSubscriptionAsync("sub-1"), Times.Once);
    }

    [Fact]
    public async Task Delete_DoesNotCancelSubscriptionOwnedByAnotherUser()
    {
        await using var db = CreateDb();
        db.PaymentSubscriptions.Add(Subscription("user-2", "sub-2", "Kontingent"));
        await db.SaveChangesAsync();

        var stripe = new Mock<IStripeService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, stripe.Object);

        var result = await controller.Delete("sub-2", default);

        Assert.IsType<NotFoundResult>(result);
        stripe.Verify(value => value.CancelSubscriptionAsync(It.IsAny<string>()), Times.Never);
        Assert.Equal("active", (await db.PaymentSubscriptions.SingleAsync()).Status);
    }

    private static PaymentSubscription Subscription(string userId, string providerSubscriptionId, string productName) => new()
    {
        UserId = userId,
        Provider = "stripe",
        ProviderSubscriptionId = providerSubscriptionId,
        ProductName = productName,
        Status = "active",
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private static ApiPaymentSubscriptionsController CreateController(
        ApplicationDbContext db,
        ClaimsPrincipal user,
        IStripeService stripe = null!,
        IMobilePayService mobilePay = null!)
    {
        return new ApiPaymentSubscriptionsController(db, stripe, mobilePay)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
