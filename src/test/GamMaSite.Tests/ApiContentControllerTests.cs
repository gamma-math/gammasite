using System.Collections.Generic;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Security.Claims;
using GamMaSite.ViewModels.Api;

namespace GamMaSite.Tests;

/*
 * Tests the /api/content endpoints used by the React event and news pages.
 * Covers published content retrieval and protection of unpublished content from anonymous users.
 */
public class ApiContentControllerTests
{
    [Fact]
    public async Task GetPublished_ReturnsPublishedItemsFromService()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        var accessControl = new Mock<IAccessControlService>();
        var item = new ContentItem { Id = 7, Title = "Public event", Type = ContentTypes.Event, Status = ContentStatuses.Published };
        content.Setup(service => service.GetPublishedAsync("EVENT", true)).ReturnsAsync(new List<ContentItem> { item });
        var controller = new ApiContentController(content.Object, registrations.Object, accessControl.Object);

        var result = await controller.GetPublished("EVENT", true);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(response.Value));
        content.Verify(service => service.GetPublishedAsync("EVENT", true), Times.Once);
    }

    [Fact]
    public async Task GetById_AnonymousUserDoesNotRequestUnpublishedContent()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        var accessControl = new Mock<IAccessControlService>();
        accessControl.Setup(service => service.HasPermissionAsync(It.IsAny<ClaimsPrincipal>(), PermissionCodes.ContentEdit))
            .ReturnsAsync(false);
        content.Setup(service => service.GetByIdAsync(7, false)).ReturnsAsync((ContentItem?)null);
        var controller = new ApiContentController(content.Object, registrations.Object, accessControl.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetById(7);

        Assert.IsType<NotFoundResult>(result);
        content.Verify(service => service.GetByIdAsync(7, false), Times.Once);
        content.Verify(service => service.GetByIdAsync(7, true), Times.Never);
    }

    [Fact]
    public async Task Create_UsesAuthenticatedUserAndReturnsCreatedResult()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        var item = new ContentItem { Id = 8, Title = "New event", Type = ContentTypes.Event, Status = ContentStatuses.Draft };
        content.Setup(service => service.CreateAsync(It.IsAny<SaveContentItemRequest>(), "admin-1"))
            .ReturnsAsync(item);
        var controller = Controller(content, registrations, TestDoubles.User("admin-1", "Admin"));

        var result = await controller.Create(new SaveContentItemRequest { Title = "New event" });

        Assert.IsType<CreatedAtActionResult>(result);
        content.Verify(service => service.CreateAsync(It.IsAny<SaveContentItemRequest>(), "admin-1"), Times.Once);
    }

    [Fact]
    public async Task Register_MapsServiceValidationErrorToBadRequest()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        registrations.Setup(service => service.RegisterAsync(7, "user-1", It.IsAny<SaveEventRegistrationRequest>()))
            .ThrowsAsync(new System.ArgumentException("Registration is closed"));
        var controller = Controller(content, registrations, TestDoubles.User("user-1"));

        var result = await controller.Register(7, new SaveEventRegistrationRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRegistration_ForbidsUsersWithoutRegistrationEditAccess()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        var accessControl = new Mock<IAccessControlService>();
        accessControl.Setup(service => service.CanEditRegistrationsAsync(It.IsAny<ClaimsPrincipal>(), 7))
            .ReturnsAsync(false);
        var controller = new ApiContentController(content.Object, registrations.Object, accessControl.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = TestDoubles.User("user-1") }
            }
        };

        var result = await controller.DeleteRegistration(7, 3);

        Assert.IsType<ForbidResult>(result);
        registrations.Verify(service => service.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UploadImage_AllowsEventOrganizer()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        var accessControl = new Mock<IAccessControlService>();
        var media = new Mock<IContentMediaService>();
        content.Setup(service => service.GetByIdAsync(7, true)).ReturnsAsync(new ContentItem { Id = 7, Type = ContentTypes.Event });
        accessControl.Setup(service => service.CanEditEventAsync(It.IsAny<ClaimsPrincipal>(), 7)).ReturnsAsync(true);
        media.Setup(service => service.UploadAsync(7, It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(new ContentMediaUploadResult { Url = "/media/content/events/event.png" });
        var controller = new ApiContentController(content.Object, registrations.Object, accessControl.Object, media.Object);

        var result = await controller.UploadImage(7, null);

        Assert.IsType<OkObjectResult>(result);
        media.Verify(service => service.UploadAsync(7, null), Times.Once);
    }

    [Fact]
    public async Task DeleteImage_ForbidsEventUserWithoutOrganizerOrContentPermission()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        var accessControl = new Mock<IAccessControlService>();
        var media = new Mock<IContentMediaService>();
        content.Setup(service => service.GetByIdAsync(7, true)).ReturnsAsync(new ContentItem { Id = 7, Type = ContentTypes.Event });
        accessControl.Setup(service => service.CanEditEventAsync(It.IsAny<ClaimsPrincipal>(), 7)).ReturnsAsync(false);
        var controller = new ApiContentController(content.Object, registrations.Object, accessControl.Object, media.Object);

        var result = await controller.DeleteImage(7);

        Assert.IsType<ForbidResult>(result);
        media.Verify(service => service.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UploadImage_UsesExistingContentPermissionForNews()
    {
        var content = new Mock<IContentService>();
        var registrations = new Mock<IEventRegistrationService>();
        var accessControl = new Mock<IAccessControlService>();
        var media = new Mock<IContentMediaService>();
        content.Setup(service => service.GetByIdAsync(8, true)).ReturnsAsync(new ContentItem { Id = 8, Type = ContentTypes.News });
        accessControl.Setup(service => service.HasPermissionAsync(It.IsAny<ClaimsPrincipal>(), PermissionCodes.ContentEdit)).ReturnsAsync(true);
        media.Setup(service => service.UploadAsync(8, It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(new ContentMediaUploadResult { Url = "/media/content/news/news.png" });
        var controller = new ApiContentController(content.Object, registrations.Object, accessControl.Object, media.Object);

        var result = await controller.UploadImage(8, null);

        Assert.IsType<OkObjectResult>(result);
    }

    private static ApiContentController Controller(Mock<IContentService> content,
        Mock<IEventRegistrationService> registrations, ClaimsPrincipal user)
    {
        return new ApiContentController(content.Object, registrations.Object, new Mock<IAccessControlService>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }
}
