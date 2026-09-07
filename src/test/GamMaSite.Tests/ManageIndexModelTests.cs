using System.Threading.Tasks;
using GamMaSite.Areas.Identity.Pages.Account.Manage;
using GamMaSite.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the ASP.NET Identity account management profile model.
 * Covers profile loading, visibility updates, and refreshing the signed-in user.
 */
public class ManageIndexModelTests
{
    [Fact]
    public async Task OnGet_LoadsProfileAndVisibility()
    {
        var user = CreateUser();
        var userManager = TestDoubles.UserManager();
        var signInManager = TestDoubles.SignInManager(userManager);
        ConfigureUserManager(userManager, user);
        var model = new IndexModel(userManager.Object, signInManager.Object)
        {
            PageContext = TestDoubles.PageContext(TestDoubles.User(user.Id))
        };

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(user.UserName, model.Username);
        Assert.Equal(user.Status, model.Status);
        Assert.Equal(user.Navn, model.Input.Navn);
        Assert.False(model.Input.Visibility);
    }

    [Fact]
    public async Task OnPost_UpdatesVisibilityAndRefreshesSignIn()
    {
        var user = CreateUser();
        var userManager = TestDoubles.UserManager();
        var signInManager = TestDoubles.SignInManager(userManager);
        ConfigureUserManager(userManager, user);
        userManager.Setup(manager => manager.SetPhoneNumberAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var model = new IndexModel(userManager.Object, signInManager.Object)
        {
            PageContext = TestDoubles.PageContext(TestDoubles.User(user.Id)),
            Input = new IndexModel.InputModel
            {
                Navn = user.Navn,
                Adresse = user.Adresse,
                Aargang = user.Aargang,
                PhoneNumber = user.PhoneNumber,
                Beskaeftigelse = user.Beskaeftigelse,
                Visibility = false
            }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(VisibilityStatus.NONVISIBLE, user.Visibility);
        userManager.Verify(manager => manager.UpdateAsync(user), Times.Once);
        signInManager.Verify(manager => manager.RefreshSignInAsync(user), Times.Once);
    }

    private static SiteUser CreateUser() => new()
    {
        Id = "user-1",
        UserName = "user@example.com",
        Email = "user@example.com",
        Navn = "Test User",
        Adresse = "Testvej 1",
        Aargang = 2020,
        PhoneNumber = "12345678",
        Beskaeftigelse = "Student",
        Status = UserStatus.BETALT,
        Visibility = VisibilityStatus.NONVISIBLE
    };

    private static void ConfigureUserManager(Mock<UserManager<SiteUser>> userManager, SiteUser user)
    {
        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(manager => manager.GetUserNameAsync(user)).ReturnsAsync(user.UserName);
        userManager.Setup(manager => manager.GetPhoneNumberAsync(user)).ReturnsAsync(user.PhoneNumber);
    }
}
