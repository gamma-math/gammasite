using System.Threading.Tasks;
using GamMaSite.Controllers;
using GamMaSite.Models;
using GamMaSite.Services;
using GamMaSite.ViewModels.Api;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the /api/email-templates endpoints used by the React admin template pages.
 * Covers missing-resource handling and conversion of service results to HTTP responses.
 */
public class ApiEmailTemplatesControllerTests
{
    [Fact]
    public async Task GetById_ReturnsNotFoundWhenTemplateDoesNotExist()
    {
        var service = new Mock<IEmailTemplateService>();
        service.Setup(value => value.GetByIdAsync(99)).ReturnsAsync((EmailTemplate?)null);

        var result = await new ApiEmailTemplatesController(service.Object).GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_MapsValidationExceptionToBadRequest()
    {
        var service = new Mock<IEmailTemplateService>();
        service.Setup(value => value.CreateAsync(It.IsAny<SaveEmailTemplateRequest>()))
            .ThrowsAsync(new System.ArgumentException("Name is required"));

        var result = await new ApiEmailTemplatesController(service.Object).Create(new SaveEmailTemplateRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundWhenTemplateDoesNotExist()
    {
        var service = new Mock<IEmailTemplateService>();
        service.Setup(value => value.UpdateAsync(99, It.IsAny<SaveEmailTemplateRequest>()))
            .ReturnsAsync((EmailTemplate?)null);

        var result = await new ApiEmailTemplatesController(service.Object).Update(99, new SaveEmailTemplateRequest());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenTemplateWasDeleted()
    {
        var service = new Mock<IEmailTemplateService>();
        service.Setup(value => value.DeleteAsync(1)).ReturnsAsync(true);

        var result = await new ApiEmailTemplatesController(service.Object).Delete(1);

        Assert.IsType<NoContentResult>(result);
    }
}
