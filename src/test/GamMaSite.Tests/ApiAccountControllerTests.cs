using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the /api/account endpoints used by the React account pages.
 * Covers login outcomes and protection against unsafe external return URLs.
 */
public class ApiAccountControllerTests
{
    [Fact]
    public async Task Login_SuccessFallsBackToReactForExternalReturnUrl()
    {
        var userManager = TestDoubles.UserManager();
        var signIn = TestDoubles.SignInManager(userManager);
        signIn.Setup(value => value.PasswordSignInAsync("user@example.com", "Password1!", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var controller = new ApiAccountController(signIn.Object, userManager.Object,
            new Mock<ISystemEmailTemplateService>().Object, new Mock<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().Object);

        var result = await controller.Login(new LoginRequest
        {
            Email = "user@example.com", Password = "Password1!", ReturnUrl = "https://evil.example/redirect"
        });

        var response = Assert.IsType<OkObjectResult>(result).Value;
        Assert.Equal("/react", response?.GetType().GetProperty("redirectUrl")?.GetValue(response));
    }

    [Fact]
    public async Task Login_LockedOutReturnsBadRequest()
    {
        var userManager = TestDoubles.UserManager();
        var signIn = TestDoubles.SignInManager(userManager);
        signIn.Setup(value => value.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var controller = new ApiAccountController(signIn.Object, userManager.Object,
            new Mock<ISystemEmailTemplateService>().Object, new Mock<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().Object);

        Assert.IsType<BadRequestObjectResult>(await controller.Login(new LoginRequest()));
    }
}
