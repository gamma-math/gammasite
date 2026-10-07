using System;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Http;
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
    public async Task PreviewRecipients_ResolvesRegisteredAttendeesForSelectedEvents()
    {
        await using var db = CreateDb();
        var attendee = User("attendee");
        var declined = User("declined");
        db.Users.AddRange(attendee, declined);
        db.EventRegistrations.AddRange(
            new EventRegistration { ContentItemId = 7, UserId = attendee.Id, Registered = true, RegistrationType = RegistrationTypes.Attendee },
            new EventRegistration { ContentItemId = 7, UserId = declined.Id, Registered = false, RegistrationType = RegistrationTypes.Declined });
        await db.SaveChangesAsync();

        var userManager = TestDoubles.UserManager();
        userManager.SetupGet(value => value.Users).Returns(db.Users);
        var controller = CreateController(db, TestDoubles.RoleManager(), userManager);

        var result = await controller.PreviewRecipients(new MessageRecipientPreviewRequest
        {
            RecipientEventIds = new[] { 7 }
        });

        var preview = Assert.IsType<MessageRecipientPreviewDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(1, preview.RecipientCount);
        Assert.Equal("attendee", Assert.Single(preview.Recipients).Name);
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

    [Fact]
    public async Task GetCategories_ReturnsStatusesAndRoles()
    {
        await using var db = CreateDb();
        db.Users.Add(User("member"));
        db.Roles.Add(new IdentityRole("Board"));
        await db.SaveChangesAsync();
        var users = TestDoubles.UserManager();
        var roles = TestDoubles.RoleManager();
        users.SetupGet(value => value.Users).Returns(db.Users);
        roles.SetupGet(value => value.Roles).Returns(db.Roles);

        var result = await CreateController(db, roles, users).GetCategories();

        var categories = Assert.IsType<MessageCategoriesDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains(UserStatus.BETALT.ToString(), categories.Statuses);
        Assert.Contains("Board", categories.Roles);
    }

    [Fact]
    public async Task PreviewRecipients_ResolvesMembersByRole()
    {
        await using var db = CreateDb();
        var member = User("board-member");
        db.Users.Add(member);
        var role = new IdentityRole("Board") { NormalizedName = "BOARD" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = member.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        var userManager = TestDoubles.UserManager();
        userManager.SetupGet(value => value.Users).Returns(db.Users);
        var roleManager = TestDoubles.RoleManager();
        var controller = CreateController(db, roleManager, userManager);

        var result = await controller.PreviewRecipients(new MessageRecipientPreviewRequest
        {
            Roles = new[] { "Board" }
        });

        var preview = Assert.IsType<MessageRecipientPreviewDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(1, preview.RecipientCount);
        Assert.Equal("board-member", Assert.Single(preview.Recipients).Name);
    }

    [Fact]
    public async Task Render_PreservesEventWallClockTime()
    {
        await using var db = CreateDb();
        db.ContentItems.Add(new ContentItem
        {
            Id = 7,
            Title = "ProfessorAften",
            Slug = "professoraften",
            Type = ContentTypes.Event,
            Status = ContentStatuses.Published,
            StartDate = new DateTime(2026, 12, 2, 18, 30, 0),
            EndDate = new DateTime(2026, 12, 2, 20, 30, 0),
            Body = "Indhold"
        });
        await db.SaveChangesAsync();

        var templates = new Mock<IEmailTemplateService>();
        templates.Setup(value => value.GetByIdAsync(1)).ReturnsAsync(new EmailTemplate
        {
            Id = 1,
            Subject = "Opdatering",
            HtmlBody = "{{EventBlocks}}"
        });
        var controller = CreateController(db, TestDoubles.RoleManager(), TestDoubles.UserManager(), templates);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Render(new RenderEmailMessageRequest
        {
            TemplateId = 1,
            SelectedEventIds = new[] { 7 }
        });

        var rendered = Assert.IsType<RenderEmailMessageDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("2. december 2026 kl. 18.30", rendered.Html);
        Assert.Contains("2. december 2026 kl. 20.30", rendered.Html);
    }

    private static ApiMessagesController CreateController(ApplicationDbContext db,
        Mock<RoleManager<IdentityRole>> roleManager, Mock<UserManager<SiteUser>> userManager,
        Mock<IEmailTemplateService> templates = null!)
    {
        templates ??= new Mock<IEmailTemplateService>();
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
