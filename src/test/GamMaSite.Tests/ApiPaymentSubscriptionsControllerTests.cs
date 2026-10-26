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
            NextChargeAt = DateTime.UtcNow.AddMonths(3),
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
        var subscription = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal(PaymentSubscriptionStatuses.Canceled, subscription.Status);
        Assert.Null(subscription.NextChargeAt);
    }

    [Fact]
    public async Task Delete_StopsCreatedMobilePayAgreementBeforeReleasingLocalPendingState()
    {
        await using var db = CreateDb();
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "agr-created",
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Pending,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.GetAgreementAsync("agr-created", It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new MobilePayAgreement("agr-created", null, "CREATED", null));
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, new Mock<IStripeService>().Object, mobilePay.Object);

        var result = await controller.Delete("agr-created", default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(PaymentSubscriptionStatuses.Canceled, ok.Value?.GetType().GetProperty("status")?.GetValue(ok.Value));
        mobilePay.Verify(value => value.StopAgreementAsync("agr-created", It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        Assert.Equal(PaymentSubscriptionStatuses.Canceled, (await db.PaymentSubscriptions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Delete_KeepsPendingStateWhenMobilePayCannotStopAgreement()
    {
        await using var db = CreateDb();
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "agr-created",
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Pending,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.GetAgreementAsync("agr-created", It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new MobilePayAgreement("agr-created", null, "CREATED", null));
        mobilePay.Setup(value => value.StopAgreementAsync("agr-created", It.IsAny<System.Threading.CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MobilePay returned 409."));
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, new Mock<IStripeService>().Object, mobilePay.Object);

        var result = await controller.Delete("agr-created", default);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, error.StatusCode);
        mobilePay.Verify(value => value.StopAgreementAsync("agr-created", It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        Assert.Equal(PaymentSubscriptionStatuses.Pending, (await db.PaymentSubscriptions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Get_PopulatesMobilePayPaymentDatesFromActiveAgreement()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser
        {
            Id = "user-1",
            UserName = "member@example.com",
            Status = UserStatus.OPRETTET
        });
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
        Assert.Equal(new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc), subscription.NextChargeAt);
        var member = await db.Users.SingleAsync(item => item.Id == "user-1");
        Assert.Equal(UserStatus.BETALT, member.Status);
        Assert.Equal(start, member.KontingentDato);

        using var responseJson = JsonDocument.Parse(JsonSerializer.Serialize(response.Value));
        Assert.Equal(12500, responseJson.RootElement[0].GetProperty("amountMinor").GetInt64());
        Assert.Equal("DKK", responseJson.RootElement[0].GetProperty("currency").GetString());
    }

    [Fact]
    public async Task Get_CorrectsExistingMobilePayAnnualNextChargeDate()
    {
        await using var db = CreateDb();
        var paidAt = new DateTime(2026, 10, 22, 11, 14, 0, DateTimeKind.Utc);
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            BillingInterval = "YEAR",
            BillingIntervalCount = 1,
            IsActive = true,
            Created = paidAt,
            Updated = paidAt
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            PaymentOfferId = offer.Id,
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "agr-active",
            ProviderProductId = offer.Id.ToString(),
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Active,
            CurrentPeriodStart = paidAt,
            LastPaidAt = paidAt,
            NextChargeAt = paidAt.AddYears(1),
            Created = paidAt,
            Updated = paidAt
        });
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.GetAgreementAsync("agr-active", It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new MobilePayAgreement("agr-active", null, "ACTIVE", "charge-1"));
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, new Mock<IStripeService>().Object, mobilePay.Object);

        await controller.Get(default);

        var subscription = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal(new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc), subscription.NextChargeAt);
    }

    [Fact]
    public async Task Get_DoesNotReviveSubscriptionCanceledAfterProviderRead()
    {
        await using var db = CreateDb();
        var start = new DateTime(2026, 10, 19, 8, 34, 0, DateTimeKind.Utc);
        var subscription = new PaymentSubscription
        {
            UserId = "user-1",
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "agr-race",
            Status = PaymentSubscriptionStatuses.Pending,
            NextChargeAt = start.AddYears(1),
            Created = start,
            Updated = start
        };
        db.PaymentSubscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.GetAgreementAsync("agr-race", It.IsAny<System.Threading.CancellationToken>()))
            .Returns(async () =>
            {
                subscription.Status = PaymentSubscriptionStatuses.Canceled;
                subscription.NextChargeAt = null;
                await db.SaveChangesAsync();
                return new MobilePayAgreement("agr-race", null, "ACTIVE", "charge-1", start, start);
            });
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test"));
        var controller = CreateController(db, user, new Mock<IStripeService>().Object, mobilePay.Object);

        var result = Assert.IsType<OkObjectResult>(await controller.Get(default));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal(PaymentSubscriptionStatuses.Canceled, json.RootElement[0].GetProperty("status").GetString());
        var current = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal(PaymentSubscriptionStatuses.Canceled, current.Status);
        Assert.Null(current.NextChargeAt);
    }

    [Fact]
    public async Task Delete_CancelsCurrentUsersStripeSubscription()
    {
        await using var db = CreateDb();
        var paymentSubscription = Subscription("user-1", "sub-1", "Kontingent");
        paymentSubscription.Provider = "Stripe";
        paymentSubscription.NextChargeAt = DateTime.UtcNow.AddMonths(3);
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
        var subscription = await db.PaymentSubscriptions.SingleAsync();
        Assert.Equal("canceled", subscription.Status);
        Assert.Null(subscription.NextChargeAt);
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
