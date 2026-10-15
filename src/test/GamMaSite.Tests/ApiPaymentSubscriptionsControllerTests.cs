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

    private static ApiPaymentSubscriptionsController CreateController(ApplicationDbContext db, ClaimsPrincipal user, IStripeService stripe = null!)
    {
        return new ApiPaymentSubscriptionsController(db, stripe)
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
