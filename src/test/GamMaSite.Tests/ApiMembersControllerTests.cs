using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the /api/members endpoints used by the React member directory and admin pages.
 * Covers public member filtering, email visibility, and admin status updates.
 */
public class ApiMembersControllerTests
{
    [Fact]
    public async Task GetMembers_ExcludesInactiveUncreatedAndUnconfirmedUsers()
    {
        await using var db = CreateDb();
        db.Users.AddRange(
            User("visible", UserStatus.BETALT, true, VisibilityStatus.VISIBLE),
            User("inactive", UserStatus.INAKTIV, true, VisibilityStatus.VISIBLE),
            User("created", UserStatus.OPRETTET, true, VisibilityStatus.VISIBLE),
            User("unconfirmed", UserStatus.BETALT, false, VisibilityStatus.VISIBLE),
            User("private", UserStatus.BETALT, true, VisibilityStatus.NONVISIBLE));
        await db.SaveChangesAsync();
        var manager = TestDoubles.UserManager();
        manager.SetupGet(value => value.Users).Returns(db.Users);

        var result = await new ApiMembersController(manager.Object).GetMembers();
        var members = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<MemberDto>>(Assert.IsType<OkObjectResult>(result).Value);

        Assert.Equal(new[] { "private", "visible" }, members.Select(member => member.Name));
        Assert.Null(members.Single(member => member.Name == "private").Email);
        Assert.Equal("visible@example.com", members.Single(member => member.Name == "visible").Email);
    }

    [Fact]
    public async Task UpdateStatus_UpdatesUserAndSetsPaymentDate()
    {
        await using var db = CreateDb();
        var user = User("member", UserStatus.SKYLDER, true, VisibilityStatus.VISIBLE);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var manager = TestDoubles.UserManager();
        manager.Setup(value => value.FindByIdAsync(user.Id)).ReturnsAsync(user);
        manager.Setup(value => value.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await new ApiMembersController(manager.Object).UpdateStatus(user.Id,
            new UpdateMemberStatusRequest { Status = UserStatus.BETALT.ToString() });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(UserStatus.BETALT, user.Status);
        Assert.NotEqual(default, user.KontingentDato);
        manager.Verify(value => value.UpdateAsync(user), Times.Once);
    }

    private static SiteUser User(string name, UserStatus status, bool confirmed, VisibilityStatus visibility) => new()
    {
        Id = name, UserName = name, Navn = name, Email = $"{name}@example.com", EmailConfirmed = confirmed,
        Status = status, Visibility = visibility
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
