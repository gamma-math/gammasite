using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the /api/payments endpoints used by the React membership payment page.
 * Covers public-key configuration and missing product responses.
 */
public class ApiPaymentsControllerTests
{
    [Fact]
    public void GetConfig_ReturnsConfiguredPublicKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["StripeConfig:PublicApiKey"] = "pk_test_value" })
            .Build();
        var controller = new ApiPaymentsController(new Mock<IStripeService>().Object, configuration);

        var result = Assert.IsType<OkObjectResult>(controller.GetConfig());

        Assert.Equal("pk_test_value", result.Value?.GetType().GetProperty("PublicApiKey")?.GetValue(result.Value));
    }

    [Fact]
    public async Task GetProduct_ReturnsNotFoundWhenStripeHasNoProduct()
    {
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetProductAsync("missing")).ReturnsAsync((Product?)null);
        var controller = new ApiPaymentsController(stripe.Object, new ConfigurationBuilder().Build());

        Assert.IsType<NotFoundResult>(await controller.GetProduct("missing"));
    }
}
