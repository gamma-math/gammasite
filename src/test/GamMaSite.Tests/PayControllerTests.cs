using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe;
using Stripe.Checkout;
using Xunit;

namespace GamMaSite.Tests;

public sealed class PayControllerTests
{
    [Theory]
    [InlineData("legacy")]
    [InlineData("iso")]
    public async Task KontingentSuccess_AcceptsInvariantSessionCreatedValue(string format)
    {
        var created = DateTime.UtcNow.AddMinutes(-5);
        var sessionCreated = format == "legacy"
            ? created.ToString(CultureInfo.InvariantCulture)
            : created.ToString("O", CultureInfo.InvariantCulture);
        var userManager = TestDoubles.UserManager();
        var user = new SiteUser { Id = "user-1", Status = UserStatus.OPRETTET };
        userManager.Setup(value => value.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(value => value.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetProductAsync("prod_membership"))
            .ReturnsAsync(new Product { Id = "prod_membership" });
        stripe.Setup(value => value.GetSessionAsync("cs_test"))
            .ReturnsAsync(new Session
            {
                ClientReferenceId = "user-1",
                PaymentStatus = "paid",
                Metadata = new Dictionary<string, string>
                {
                    ["Product"] = "prod_membership",
                    ["SessionCreated"] = sessionCreated
                }
            });
        stripe.Setup(value => value.IsPaymentComplete(It.IsAny<Session>())).Returns(true);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:MembershipProductId"] = "prod_membership"
            })
            .Build();
        var controller = new PayController(userManager.Object, stripe.Object, configuration);

        var result = await controller.KontingentSuccessAsync("cs_test");

        Assert.IsType<ViewResult>(result);
        Assert.Equal(UserStatus.BETALT, user.Status);
    }
}
