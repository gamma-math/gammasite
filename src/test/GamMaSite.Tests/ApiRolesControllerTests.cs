using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using GamMaSite.Controllers;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task GetRoles_ReturnsRolesOrderedByName()
    {
        await using var db = new GamMaSite.Data.ApplicationDbContext(new DbContextOptionsBuilder<GamMaSite.Data.ApplicationDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString()).Options);
        db.Roles.AddRange(new IdentityRole("Zeta"), new IdentityRole("Alpha"));
        await db.SaveChangesAsync();
        var roles = TestDoubles.RoleManager();
        roles.SetupGet(value => value.Roles).Returns(db.Roles);

        var result = await new ApiRolesController(roles.Object, TestDoubles.UserManager().Object).GetRoles();
        var values = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<RoleDto>>(Assert.IsType<OkObjectResult>(result).Value);

        Assert.Equal(new[] { "Alpha", "Zeta" }, values.Select(value => value.Name));
    }

    [Fact]
    public async Task UpdatePermissions_KeepsRequiredPermissionsForAdmin()
    {
        await using var db = new GamMaSite.Data.ApplicationDbContext(new DbContextOptionsBuilder<GamMaSite.Data.ApplicationDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString()).Options);
        db.Permissions.AddRange(
            new Permission { Id = 1, Code = PermissionCodes.ContentEdit },
            new Permission { Id = 2, Code = PermissionCodes.RegistrationsEdit },
            new Permission { Id = 3, Code = PermissionCodes.MessagesEdit },
            new Permission { Id = 4, Code = PermissionCodes.RolesEdit },
            new Permission { Id = 5, Code = PermissionCodes.FinanceViewAll });
        await db.SaveChangesAsync();

        var role = new IdentityRole { Id = "admin", Name = "ADMIN" };
        var roles = TestDoubles.RoleManager();
        roles.Setup(value => value.FindByIdAsync("admin")).ReturnsAsync(role);
        roles.Setup(value => value.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());
        roles.Setup(value => value.AddClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);
        var controller = new ApiRolesController(roles.Object, TestDoubles.UserManager().Object, db);

        await controller.UpdatePermissions("admin", new UpdateRolePermissionsRequest
        {
            PermissionCodes = new[] { PermissionCodes.FinanceViewAll }
        });

        var enabled = await db.RolePermissions.Include(item => item.Permission).Select(item => item.Permission.Code).ToListAsync();
        Assert.Contains(PermissionCodes.ContentEdit, enabled);
        Assert.Contains(PermissionCodes.RegistrationsEdit, enabled);
        Assert.Contains(PermissionCodes.MessagesEdit, enabled);
        Assert.Contains(PermissionCodes.RolesEdit, enabled);
        Assert.Contains(PermissionCodes.FinanceViewAll, enabled);
    }
}
