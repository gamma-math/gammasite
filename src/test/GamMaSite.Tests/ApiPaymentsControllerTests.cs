using System.Collections.Generic;
using GamMaSite.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GamMaSite.Tests;

public sealed class ApiPaymentsControllerTests
{
    [Fact]
    public void GetConfig_ReturnsConfiguredPublicKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:PublicApiKey"] = "pk_test_value"
            })
            .Build();
        var controller = new ApiPaymentsController(configuration);

        var result = Assert.IsType<OkObjectResult>(controller.GetConfig());

        Assert.Equal(
            "pk_test_value",
            result.Value?.GetType().GetProperty("PublicApiKey")?.GetValue(result.Value));
    }
}
