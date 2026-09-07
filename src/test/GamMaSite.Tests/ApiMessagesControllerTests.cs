using System;
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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the /api/messages endpoints used by the React admin messaging page.
 * Covers selected-member recipient resolution, deduplication, and empty-recipient validation.
 */
public class ApiMessagesControllerTests
{
    [Fact]
    public async Task PreviewRecipients_ResolvesSpecificMembersAndDeduplicatesThem()
    {
        await using var db = CreateDb();
        var first = User("first");
        var second = User("second");
        db.Users.AddRange(first, second);
        await db.SaveChangesAsync();

        var userManager = TestDoubles.UserManager();
        userManager.SetupGet(value => value.Users).Returns(db.Users);
        var roleManager = TestDoubles.RoleManager();
        var controller = CreateController(db, roleManager, userManager);

        var result = await controller.PreviewRecipients(new MessageRecipientPreviewRequest
        {
            RecipientMemberIds = new[] { first.Id, first.Id, second.Id }
        });

        var preview = Assert.IsType<MessageRecipientPreviewDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, preview.RecipientCount);
        Assert.Equal(2, preview.EmailCount);
        Assert.Equal(new[] { "first", "second" }, preview.Recipients.Select(item => item.Name));
    }

    [Fact]
    public async Task Send_ReturnsBadRequestWhenNoRecipientsMatch()
    {
        await using var db = CreateDb();
        var userManager = TestDoubles.UserManager();
        userManager.SetupGet(value => value.Users).Returns(db.Users);
        var controller = CreateController(db, TestDoubles.RoleManager(), userManager);

        var result = await controller.Send(new SendEmailMessageRequest
        {
            Subject = "Subject", Html = "<p>Body</p>", Channel = MessageMedia.Email.ToString(),
            RecipientMemberIds = new[] { "missing" }
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ingen modtagere", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static ApiMessagesController CreateController(ApplicationDbContext db,
        Mock<RoleManager<IdentityRole>> roleManager, Mock<UserManager<SiteUser>> userManager)
    {
        var templates = new Mock<IEmailTemplateService>();
        var email = new Mock<IEmailService>();
        var sms = new Mock<ISmsSender>();
        return new ApiMessagesController(db, roleManager.Object, userManager.Object, templates.Object,
            email.Object, sms.Object, NullLogger<ApiMessagesController>.Instance);
    }

    private static SiteUser User(string name) => new()
    {
        Id = name, UserName = name, Navn = name, Email = $"{name}@example.com", PhoneNumber = "12345678",
        Status = UserStatus.BETALT, EmailConfirmed = true
    };

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
