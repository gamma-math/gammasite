using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the /api/roles endpoints used by the React admin role pages.
 * Covers required role names and protection of the built-in ADMIN role.
 */
public class ApiRolesControllerTests
{
    [Fact]
    public async Task Create_RejectsBlankRoleName()
    {
        var controller = new ApiRolesController(TestDoubles.RoleManager().Object, TestDoubles.UserManager().Object);

        var result = await controller.Create(new SaveRoleRequest { Name = " " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_RejectsBuiltInAdminRole()
    {
        var role = new IdentityRole { Id = "admin", Name = "ADMIN" };
        var roles = TestDoubles.RoleManager();
        roles.Setup(value => value.FindByIdAsync("admin")).ReturnsAsync(role);
        var controller = new ApiRolesController(roles.Object, TestDoubles.UserManager().Object);

        var result = await controller.Delete("admin");

        Assert.IsType<BadRequestObjectResult>(result);
        roles.Verify(value => value.DeleteAsync(It.IsAny<IdentityRole>()), Times.Never);
    }
}
