using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).GetMembers();
        var members = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<MemberDto>>(Assert.IsType<OkObjectResult>(result).Value);

        Assert.Equal(new[] { "private", "visible" }, members.Select(member => member.Name));
        Assert.Null(members.Single(member => member.Name == "private").Email);
        Assert.Equal("visible@example.com", members.Single(member => member.Name == "visible").Email);
    }

    [Fact]
    public async Task GetFinanceMembers_ReturnsOnlyIdsAndNames()
    {
        await using var db = CreateDb();
        var user = User("finance-user", UserStatus.BETALT, true, VisibilityStatus.VISIBLE);
        user.PhoneNumber = "+4512345678";
        user.Beskaeftigelse = "Revisor";
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var manager = TestDoubles.UserManager();
        manager.SetupGet(value => value.Users).Returns(db.Users);

        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).GetFinanceMembers();
        var member = Assert.Single(Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<FinanceMemberDto>>(Assert.IsType<OkObjectResult>(result).Value));

        Assert.Equal(user.Id, member.Id);
        Assert.Equal(user.Navn, member.Name);
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

        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).UpdateStatus(user.Id,
            new UpdateMemberStatusRequest { Status = UserStatus.BETALT.ToString() });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(UserStatus.BETALT, user.Status);
        Assert.NotEqual(default, user.KontingentDato);
        manager.Verify(value => value.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_RejectsInvalidStatus()
    {
        var manager = TestDoubles.UserManager();

        await using var db = CreateDb();
        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).UpdateStatus("member",
            new UpdateMemberStatusRequest { Status = "INVALID" });

        Assert.IsType<BadRequestObjectResult>(result);
        manager.Verify(value => value.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFoundForUnknownUser()
    {
        var manager = TestDoubles.UserManager();
        manager.Setup(value => value.FindByIdAsync("missing")).ReturnsAsync((SiteUser?)null);

        await using var db = CreateDb();
        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).UpdateStatus("missing",
            new UpdateMemberStatusRequest { Status = UserStatus.BETALT.ToString() });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateMassStatus_UpdatesUsersInDateRange()
    {
        await using var db = CreateDb();
        var first = User("first", UserStatus.SKYLDER, true, VisibilityStatus.VISIBLE);
        first.KontingentDato = new DateTime(2025, 1, 1);
        var second = User("second", UserStatus.BETALT, true, VisibilityStatus.VISIBLE);
        second.KontingentDato = new DateTime(2025, 2, 1);
        db.Users.AddRange(first, second);
        await db.SaveChangesAsync();
        var manager = TestDoubles.UserManager();
        manager.SetupGet(value => value.Users).Returns(db.Users);
        manager.Setup(value => value.UpdateAsync(It.IsAny<SiteUser>())).ReturnsAsync(IdentityResult.Success);

        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).UpdateMassStatus(new MassUpdateMemberStatusRequest
        {
            From = new DateTime(2025, 1, 1), To = new DateTime(2025, 2, 1), Status = UserStatus.BETALT.ToString()
        });

        var response = Assert.IsType<MassUpdateMemberStatusResult>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(1, response.Updated);
        Assert.Equal(UserStatus.BETALT, first.Status);
    }

    [Fact]
    public async Task GetAdminMembers_ReportsActiveSubscription()
    {
        await using var db = CreateDb();
        var active = User("active", UserStatus.BETALT, true, VisibilityStatus.VISIBLE);
        var inactive = User("inactive-subscription", UserStatus.SKYLDER, true, VisibilityStatus.VISIBLE);
        db.Users.AddRange(active, inactive);
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = active.Id,
            Provider = "Stripe",
            ProviderSubscriptionId = "sub_active",
            Status = "active"
        });
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = inactive.Id,
            Provider = "Stripe",
            ProviderSubscriptionId = "sub_canceled",
            Status = "canceled"
        });
        await db.SaveChangesAsync();
        var manager = TestDoubles.UserManager();
        manager.SetupGet(value => value.Users).Returns(db.Users);

        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).GetAdminMembers();
        var members = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<MemberDto>>(Assert.IsType<OkObjectResult>(result).Value);

        Assert.True(members.Single(member => member.Id == active.Id).HasActiveSubscription == true);
        Assert.False(members.Single(member => member.Id == inactive.Id).HasActiveSubscription == true);
    }

    [Fact]
    public async Task GetUserSubscriptions_ReturnsOnlyActiveSubscriptionsForMember()
    {
        await using var db = CreateDb();
        var member = User("member-with-subscription", UserStatus.BETALT, true, VisibilityStatus.VISIBLE);
        db.Users.Add(member);
        db.PaymentSubscriptions.AddRange(
            new PaymentSubscription
            {
                UserId = member.Id,
                Provider = "Stripe",
                ProviderSubscriptionId = "sub_active",
                ProductName = "Kontingent",
                Status = PaymentSubscriptionStatuses.Active,
                Created = new DateTime(2025, 1, 1),
                Updated = new DateTime(2025, 1, 2)
            },
            new PaymentSubscription
            {
                UserId = member.Id,
                Provider = "Stripe",
                ProviderSubscriptionId = "sub_canceled",
                Status = "canceled"
            });
        await db.SaveChangesAsync();
        var manager = TestDoubles.UserManager();
        manager.SetupGet(value => value.Users).Returns(db.Users);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StripeConfig:AccountId"] = "acct_test",
                ["StripeConfig:Environment"] = "test"
            })
            .Build();

        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db, configuration)
            .GetUserSubscriptions(member.Id);

        var response = Assert.IsType<AdminMemberSubscriptionsDto>(Assert.IsType<OkObjectResult>(result).Value);
        var subscription = Assert.Single(response.Subscriptions);
        Assert.Equal(member.Navn, response.MemberName);
        Assert.Equal("sub_active", subscription.ProviderSubscriptionId);
        Assert.Equal("Kontingent", subscription.ProductName);
        Assert.Equal("https://dashboard.stripe.com/acct_test/test/subscriptions/sub_active", subscription.ProviderDashboardUrl);
    }

    [Fact]
    public async Task UpdateMassStatus_CanExcludeActiveSubscriptions()
    {
        await using var db = CreateDb();
        var first = User("first", UserStatus.SKYLDER, true, VisibilityStatus.VISIBLE);
        first.KontingentDato = new DateTime(2025, 1, 1);
        var subscribed = User("subscribed", UserStatus.SKYLDER, true, VisibilityStatus.VISIBLE);
        subscribed.KontingentDato = new DateTime(2025, 1, 1);
        db.Users.AddRange(first, subscribed);
        db.PaymentSubscriptions.Add(new PaymentSubscription
        {
            UserId = subscribed.Id,
            Provider = "Stripe",
            ProviderSubscriptionId = "sub_active",
            Status = "trialing"
        });
        await db.SaveChangesAsync();
        var manager = TestDoubles.UserManager();
        manager.SetupGet(value => value.Users).Returns(db.Users);
        manager.Setup(value => value.UpdateAsync(It.IsAny<SiteUser>())).ReturnsAsync(IdentityResult.Success);

        var result = await new ApiMembersController(manager.Object, new Mock<IAccessControlService>().Object, db).UpdateMassStatus(new MassUpdateMemberStatusRequest
        {
            From = new DateTime(2025, 1, 1), To = new DateTime(2025, 1, 1), Status = UserStatus.BETALT.ToString(),
            ExcludeActiveSubscriptions = true
        });

        var response = Assert.IsType<MassUpdateMemberStatusResult>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(1, response.Updated);
        Assert.Equal(UserStatus.BETALT, first.Status);
        Assert.Equal(UserStatus.SKYLDER, subscribed.Status);
    }

    private static SiteUser User(string name, UserStatus status, bool confirmed, VisibilityStatus visibility) => new()
    {
        Id = name, UserName = name, Navn = name, Email = $"{name}@example.com", EmailConfirmed = confirmed,
        Status = status, Visibility = visibility
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
