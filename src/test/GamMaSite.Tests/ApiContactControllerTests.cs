using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

public class ApiContactControllerTests
{
    [Fact]
    public async Task Send_RoutesToContactResponsibleUsers()
    {
        var contactRole = new IdentityRole("Kontakt Ansvarlig");
        var users = CreateUserManager();
        users.Setup(manager => manager.GetUsersInRoleAsync("Kontakt Ansvarlig"))
            .ReturnsAsync(new List<SiteUser> { new() { Email = "contact@gam-ma.dk" } });
        var roles = CreateRoleManager();
        roles.Setup(manager => manager.FindByNameAsync("Kontakt Ansvarlig")).ReturnsAsync(contactRole);
        var templates = new Mock<ISystemEmailTemplateService>();
        templates.Setup(service => service.SendContactRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        templates.Setup(service => service.SendContactConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var controller = new ApiContactController(users.Object, roles.Object, templates.Object);

        var result = await controller.Send(ValidRequest());

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        templates.Verify(service => service.SendContactRequestAsync(
            It.Is<IEnumerable<string>>(addresses => addresses.Count() == 1 && addresses.First() == "contact@gam-ma.dk"),
            "Arrangement", "Sommerfest", "member@example.com", "Hej"), Times.Once);
        templates.Verify(service => service.SendContactConfirmationAsync("member@example.com", "Arrangement", "Sommerfest", "Hej"), Times.Once);
        users.Verify(manager => manager.GetUsersInRoleAsync("Admin"), Times.Never);
    }

    [Fact]
    public async Task Send_FallsBackToAdminsWhenContactRoleHasNoRecipients()
    {
        var users = CreateUserManager();
        users.Setup(manager => manager.GetUsersInRoleAsync("Kontakt Ansvarlig")).ReturnsAsync(new List<SiteUser>());
        users.Setup(manager => manager.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(new List<SiteUser> { new() { Email = "admin@gam-ma.dk" } });
        var roles = CreateRoleManager();
        roles.Setup(manager => manager.FindByNameAsync("Kontakt Ansvarlig")).ReturnsAsync(new IdentityRole("Kontakt Ansvarlig"));
        var templates = new Mock<ISystemEmailTemplateService>();
        templates.Setup(service => service.SendContactRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        templates.Setup(service => service.SendContactConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var controller = new ApiContactController(users.Object, roles.Object, templates.Object);

        var result = await controller.Send(ValidRequest());

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        templates.Verify(service => service.SendContactRequestAsync(
            It.Is<IEnumerable<string>>(addresses => addresses.Count() == 1 && addresses.First() == "admin@gam-ma.dk"),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Send_DoesNotSendConfirmationWhenContactRequestTemplateIsInactive()
    {
        var users = CreateUserManager();
        users.Setup(manager => manager.GetUsersInRoleAsync("Kontakt Ansvarlig"))
            .ReturnsAsync(new List<SiteUser> { new() { Email = "contact@gam-ma.dk" } });
        var roles = CreateRoleManager();
        roles.Setup(manager => manager.FindByNameAsync("Kontakt Ansvarlig")).ReturnsAsync(new IdentityRole("Kontakt Ansvarlig"));
        var templates = new Mock<ISystemEmailTemplateService>();
        templates.Setup(service => service.SendContactRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        var controller = new ApiContactController(users.Object, roles.Object, templates.Object);

        var result = await controller.Send(ValidRequest());

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        templates.Verify(service => service.SendContactConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static ContactRequestDto ValidRequest() => new()
    {
        Email = "member@example.com", ContactType = "Arrangement", Topic = "Sommerfest", Message = "Hej"
    };

    private static Mock<UserManager<SiteUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<SiteUser>>();
        return new Mock<UserManager<SiteUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<IdentityRole>> CreateRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, new IRoleValidator<IdentityRole>[0], null!, null!, Mock.Of<ILogger<RoleManager<IdentityRole>>>());
    }
}
