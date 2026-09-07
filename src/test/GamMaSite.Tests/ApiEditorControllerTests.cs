using System.Threading.Tasks;
using GamMaSite.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GamMaSite.Tests;

/*
 * Tests the /api/editor/images endpoint used by the React rich text editor.
 * Covers rejection of empty uploads and unsupported file extensions/content types.
 */
public class ApiEditorControllerTests
{
    [Fact]
    public async Task UploadImage_RejectsEmptyFile()
    {
        var environment = new Mock<IWebHostEnvironment>();
        var controller = new ApiEditorController(environment.Object);

        var result = await controller.UploadImage(null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadImage_RejectsDisallowedFileType()
    {
        var environment = new Mock<IWebHostEnvironment>();
        var controller = new ApiEditorController(environment.Object);
        var file = new FormFile(new System.IO.MemoryStream(new byte[] { 1 }), 0, 1, "file", "malware.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };

        var result = await controller.UploadImage(file);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
