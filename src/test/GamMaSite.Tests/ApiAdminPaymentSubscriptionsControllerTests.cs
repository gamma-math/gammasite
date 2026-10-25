using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

public sealed class ApiAdminPaymentSubscriptionsControllerTests
{
    [Fact]
    public async Task Get_ReturnsOperationalIdsAndPeriodDetails()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", Navn = "Test Bruger", Email = "user@example.com" });
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            PaymentOfferId = offer.Id,
            Provider = PaymentOfferProviders.Stripe,
            ProviderSubscriptionId = "sub_test",
            ProviderProductId = "prod_test",
            ProviderPriceId = "price_test",
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Active,
            CurrentPeriodStart = new DateTime(2026, 10, 1),
            CurrentPeriodEnd = new DateTime(2027, 10, 1),
            Created = new DateTime(2026, 10, 1),
            Updated = new DateTime(2026, 10, 2),
            LastProcessedWebhookId = "evt_test"
        });
        await db.SaveChangesAsync();

        var result = await new ApiAdminPaymentSubscriptionsController(db).Get(default);

        var subscriptions = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<AdminPaymentSubscriptionDto>>(
            Assert.IsType<OkObjectResult>(result).Value);
        var subscription = Assert.Single(subscriptions);
        Assert.Equal("user-1", subscription.UserId);
        Assert.Equal(offer.Id, subscription.PaymentOfferId);
        Assert.Equal("sub_test", subscription.ProviderSubscriptionId);
        Assert.Equal("prod_test", subscription.ProviderProductId);
        Assert.Equal("price_test", subscription.ProviderPriceId);
        Assert.Equal("evt_test", subscription.LastProcessedWebhookId);
        Assert.Equal(new DateTime(2026, 10, 1), subscription.CurrentPeriodStart);
    }

    [Fact]
    public async Task GetById_ReturnsSubscriptionOverviewAndMobilePayCharges()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", Navn = "Test Bruger", Email = "user@example.com" });
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.MobilePay,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var subscription = new PaymentSubscription
        {
            UserId = "user-1",
            PaymentOfferId = offer.Id,
            Provider = PaymentOfferProviders.MobilePay,
            ProviderSubscriptionId = "agr_test",
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Active,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentSubscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var due = DateTime.UtcNow.AddDays(30);
        var mobilePay = new Mock<IMobilePayService>();
        mobilePay.Setup(value => value.GetRecurringChargesAsync(
                "agr_test",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MobilePayCharge(
                    "charge_test",
                    "agr_test",
                    "PENDING",
                    "RECURRING",
                    15000,
                    "DKK",
                    "Kontingent",
                    due)
            });

        var result = await new ApiAdminPaymentSubscriptionsController(
            db,
            mobilePayService: mobilePay.Object)
            .GetById(subscription.Id, default);

        var response = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<AdminPaymentSubscriptionDetailDto>(response.Value);
        Assert.Equal(subscription.Id, detail.Subscription.Id);
        Assert.Equal("Test Bruger", detail.Subscription.UserName);
        var payment = Assert.Single(detail.Payments);
        Assert.Equal("charge_test", payment.Id);
        Assert.Equal(15000, payment.AmountMinor);
        Assert.Equal("PENDING", payment.Status);
        Assert.Null(detail.PaymentsError);
    }

    [Fact]
    public async Task GetById_ReturnsStripeInvoicesAsSubscriptionPayments()
    {
        await using var db = CreateDb();
        db.Users.Add(new SiteUser { Id = "user-1", Navn = "Test Bruger", Email = "user@example.com" });
        var offer = new PaymentOffer
        {
            Scope = PaymentOfferScopes.Membership,
            Provider = PaymentOfferProviders.Stripe,
            OfferType = PaymentOfferTypes.Recurring,
            ProductName = "Kontingent",
            AmountMinor = 15000,
            Currency = "DKK",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentOffers.Add(offer);
        await db.SaveChangesAsync();

        var subscription = new PaymentSubscription
        {
            UserId = "user-1",
            PaymentOfferId = offer.Id,
            Provider = PaymentOfferProviders.Stripe,
            ProviderSubscriptionId = "sub_test",
            ProductName = "Kontingent",
            Status = PaymentSubscriptionStatuses.Active,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        db.PaymentSubscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var created = DateTime.UtcNow.AddDays(-1);
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetSubscriptionPaymentsAsync("sub_test"))
            .ReturnsAsync(new[]
            {
                new StripeSubscriptionPayment(
                    "in_test",
                    "paid",
                    15000,
                    "dkk",
                    "Kontingent",
                    created,
                    created,
                    created)
            });

        var result = await new ApiAdminPaymentSubscriptionsController(
            db,
            stripeService: stripe.Object)
            .GetById(subscription.Id, default);

        var response = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<AdminPaymentSubscriptionDetailDto>(response.Value);
        var payment = Assert.Single(detail.Payments);
        Assert.Equal("in_test", payment.Id);
        Assert.Equal("INVOICE", payment.Type);
        Assert.Equal("paid", payment.Status);
        Assert.Equal(15000, payment.AmountMinor);
        Assert.Equal(created, payment.PaidAt);
        Assert.Null(detail.PaymentsError);
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
