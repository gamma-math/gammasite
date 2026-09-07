using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Areas.Identity.Pages.Account;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the ASP.NET Identity registration page model.
 * Covers input validation and prevents user creation or email sending for invalid submissions.
 */
public class RegisterModelTests
{
    [Fact]
    public void InputModel_RejectsInvalidEmailAndYear()
    {
        var input = new RegisterModel.InputModel
        {
            Navn = "Test User",
            Email = "not-an-email",
            Password = "short",
            ConfirmPassword = "different",
            PhoneNumber = "12345678",
            Aargang = 1800,
            Beskaeftigelse = "Student",
            Adresse = "Testvej 1"
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(input, new ValidationContext(input), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterModel.InputModel.Email)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterModel.InputModel.Aargang)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterModel.InputModel.ConfirmPassword)));
    }

    [Fact]
    public async Task OnPost_InvalidModel_ReturnsPageWithoutCreatingUser()
    {
        var userManager = TestDoubles.UserManager();
        var signInManager = TestDoubles.SignInManager(userManager);
        signInManager.Setup(manager => manager.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(Enumerable.Empty<AuthenticationScheme>());
        var emailService = new Mock<ISystemEmailTemplateService>();
        var model = new RegisterModel(userManager.Object, signInManager.Object, new Mock<ILogger<RegisterModel>>().Object, emailService.Object)
        {
            Input = new RegisterModel.InputModel()
        };
        model.ModelState.AddModelError("Input.Email", "Email is required");

        var result = await model.OnPostAsync("/react");

        Assert.IsType<PageResult>(result);
        userManager.Verify(manager => manager.CreateAsync(It.IsAny<SiteUser>(), It.IsAny<string>()), Times.Never);
        emailService.Verify(service => service.SendRegistrationConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
