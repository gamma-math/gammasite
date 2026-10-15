using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe;
using GamMaSite.ViewModels.Api;
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

    [Fact]
    public async Task GetProducts_ReturnsConfiguredMembershipProductAndPrice()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:MembershipProductId"] = "prod_sandbox",
                ["StripeConfig:PaymentOneTimeId"] = "price_one_time"
            })
            .Build();
        var stripe = new Mock<IStripeService>();
        stripe.Setup(value => value.GetProductAsync("prod_sandbox"))
            .ReturnsAsync(new Product { Id = "prod_sandbox", Name = "Kontingent" });
        stripe.Setup(value => value.GetPriceByIdAsync("price_one_time"))
            .ReturnsAsync(new Price
            {
                Id = "price_one_time",
                ProductId = "prod_sandbox",
                UnitAmount = 15000,
                Currency = "dkk"
            });

        var controller = new ApiPaymentsController(stripe.Object, configuration);

        var result = Assert.IsType<OkObjectResult>(await controller.GetProducts());
        var products = Assert.IsType<PaymentProductDto[]>(result.Value);

        var product = Assert.Single(products);
        Assert.Equal("Kontingent", product.Name);
        Assert.Equal(15000, product.UnitAmount);
        Assert.Equal("dkk", product.Currency);
    }

    [Fact]
    public async Task GetProduct_RejectsProductFromAnotherEnvironment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:MembershipProductId"] = "prod_sandbox",
                ["StripeConfig:PaymentOneTimeId"] = "price_one_time"
            })
            .Build();
        var controller = new ApiPaymentsController(new Mock<IStripeService>().Object, configuration);

        Assert.IsType<NotFoundResult>(await controller.GetProduct("prod_production"));
    }
}
