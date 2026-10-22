using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value));
        var subscription = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal("user-1", subscription.GetProperty("userId").GetString());
        Assert.Equal(offer.Id, subscription.GetProperty("paymentOfferId").GetInt32());
        Assert.Equal("sub_test", subscription.GetProperty("providerSubscriptionId").GetString());
        Assert.Equal("prod_test", subscription.GetProperty("providerProductId").GetString());
        Assert.Equal("price_test", subscription.GetProperty("providerPriceId").GetString());
        Assert.Equal("evt_test", subscription.GetProperty("lastProcessedWebhookId").GetString());
        Assert.Contains("2026-10-01", subscription.GetProperty("currentPeriodStart").GetString());
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
