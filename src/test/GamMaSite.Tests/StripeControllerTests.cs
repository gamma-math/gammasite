using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

public sealed class StripeControllerTests
{
    [Fact]
    public async Task Product_RejectsSecondActiveRecurringSubscription()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = "user-1",
            Provider = "Stripe",
            ProviderSubscriptionId = "sub-existing",
            Status = "active",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:MembershipProductId"] = "prod_membership",
                ["StripeConfig:PaymentRecurringId"] = "price_recurring"
            })
            .Build();
        var controller = new StripeController(new Mock<IStripeService>().Object, configuration, db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
                        "Test"))
                }
            }
        };

        var result = await controller.ProductAsync("prod_membership", "another-user", "recurring");

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Du har allerede betalt medlemskab.", conflict.Value);
    }
}
