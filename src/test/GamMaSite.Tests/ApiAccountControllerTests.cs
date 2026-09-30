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

    [Fact]
    public async Task Logout_SignsOutAndReturnsReactRedirect()
    {
        var userManager = TestDoubles.UserManager();
        var signIn = TestDoubles.SignInManager(userManager);
        var controller = new ApiAccountController(signIn.Object, userManager.Object,
            new Mock<ISystemEmailTemplateService>().Object, new Mock<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().Object);

        var result = await controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        signIn.Verify(value => value.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task GetProfile_ReturnsUnauthorizedWhenIdentityUserIsMissing()
    {
        var userManager = TestDoubles.UserManager();
        userManager.Setup(value => value.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((SiteUser?)null);
        var controller = AccountController(userManager, TestDoubles.SignInManager(userManager));

        Assert.IsType<UnauthorizedResult>(await controller.GetProfile());
    }

    [Fact]
    public async Task UpdateProfile_UpdatesStudentStatusAndRefreshesSignIn()
    {
        var user = new SiteUser { Id = "user-1", UserName = "user@example.com", Status = UserStatus.BETALT };
        var userManager = TestDoubles.UserManager();
        var signIn = TestDoubles.SignInManager(userManager);
        ConfigureUser(userManager, user);
        userManager.Setup(value => value.GetPhoneNumberAsync(user)).ReturnsAsync(user.PhoneNumber);
        userManager.Setup(value => value.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var controller = AccountController(userManager, signIn);

        var result = await controller.UpdateProfile(new UpdateProfileRequest
        {
            Navn = "Updated", PhoneNumber = user.PhoneNumber, IsStudent = true, Visibility = true
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Updated", user.Navn);
        Assert.Equal(UserStatus.STUDERENDE, user.Status);
        signIn.Verify(value => value.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_RejectsUserWithoutLocalPassword()
    {
        var user = new SiteUser { Id = "user-1" };
        var userManager = TestDoubles.UserManager();
        var signIn = TestDoubles.SignInManager(userManager);
        ConfigureUser(userManager, user);
        userManager.Setup(value => value.HasPasswordAsync(user)).ReturnsAsync(false);
        var controller = AccountController(userManager, signIn);

        Assert.IsType<BadRequestObjectResult>(await controller.ChangePassword(new ChangePasswordRequest()));
    }

    [Fact]
    public async Task GetProfile_ReturnsMappedProfileForAuthenticatedUser()
    {
        var user = new SiteUser { Id = "user-1", UserName = "user@example.com", Email = "user@example.com", Status = UserStatus.BETALT, Visibility = VisibilityStatus.VISIBLE };
        var users = TestDoubles.UserManager();
        var signIn = TestDoubles.SignInManager(users);
        ConfigureUser(users, user);
        users.Setup(value => value.GetUserNameAsync(user)).ReturnsAsync(user.UserName);
        users.Setup(value => value.GetEmailAsync(user)).ReturnsAsync(user.Email);
        users.Setup(value => value.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        users.Setup(value => value.GetPhoneNumberAsync(user)).ReturnsAsync("12345678");

        var result = await AccountController(users, signIn).GetProfile();

        var response = Assert.IsType<OkObjectResult>(result).Value;
        Assert.Equal(user.UserName, response?.GetType().GetProperty("username")?.GetValue(response));
    }

    [Fact]
    public async Task ChangeEmail_ReturnsSuccessWhenEmailIsUnchanged()
    {
        var user = new SiteUser { Id = "user-1", Email = "user@example.com" };
        var users = TestDoubles.UserManager();
        ConfigureUser(users, user);
        users.Setup(value => value.GetEmailAsync(user)).ReturnsAsync(user.Email);

        var result = await AccountController(users, TestDoubles.SignInManager(users)).ChangeEmail(
            new ChangeEmailRequest { NewEmail = user.Email });

        Assert.IsType<OkObjectResult>(result);
        users.Verify(value => value.GenerateChangeEmailTokenAsync(It.IsAny<SiteUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_DoesNotSendEmailForUnconfirmedUser()
    {
        var user = new SiteUser { Id = "user-1", Email = "user@example.com" };
        var users = TestDoubles.UserManager();
        var systemEmail = new Mock<ISystemEmailTemplateService>();
        users.Setup(value => value.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        users.Setup(value => value.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
        var controller = new ApiAccountController(TestDoubles.SignInManager(users).Object, users.Object,
            systemEmail.Object, new Mock<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().Object);

        Assert.IsType<OkObjectResult>(await controller.ForgotPassword(new EmailRequest { Email = user.Email }));
        systemEmail.Verify(value => value.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static ApiAccountController AccountController(Mock<UserManager<SiteUser>> users,
        Mock<SignInManager<SiteUser>> signIn)
    {
        return new ApiAccountController(signIn.Object, users.Object,
            new Mock<ISystemEmailTemplateService>().Object,
            new Mock<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = TestDoubles.User("user-1"), Request = { Scheme = "https", Host = new Microsoft.AspNetCore.Http.HostString("localhost") }
            } }
        };
    }

    private static void ConfigureUser(Mock<UserManager<SiteUser>> manager, SiteUser user)
    {
        manager.Setup(value => value.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
    }
}
