using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    private static ApiPaymentSubscriptionsController CreateController(ApplicationDbContext db, ClaimsPrincipal user)
    {
        return new ApiPaymentSubscriptionsController(db)
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
