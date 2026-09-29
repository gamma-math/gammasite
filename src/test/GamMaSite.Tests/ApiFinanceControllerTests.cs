using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GamMaSite.Tests;

public sealed class ApiFinanceControllerTests
{
    [Fact]
    public async Task GetOverview_RejectsAnonymousUser()
    {
        var controller = Controller(new DefaultHttpContext());

        var result = await controller.GetOverview(null, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetOverview_RejectsUnsupportedHistoricalYear()
    {
        var context = new DefaultHttpContext { User = TestDoubles.User("user-1") };
        var controller = Controller(context);

        var result = await controller.GetOverview(DateTime.Today.Year - 2, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAdminOverview_RejectsYearOutsideSupportedRange()
    {
        var controller = Controller(new DefaultHttpContext());

        var result = await controller.GetAdminOverview(1899, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAdminPostings_RejectsYearOutsideSupportedRange()
    {
        var controller = Controller(new DefaultHttpContext());

        var result = await controller.GetAdminPostings(1899, false, null, null, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("bank", "eksempel-bankkontoudtog.csv", "Dato;Tekst;Bel")]
    [InlineData("mobilepay", "eksempel-mobilepay-transaktioner.csv", "Date;Timestamp;Amount")]
    public void DownloadImportTemplate_ReturnsUtf8Csv(string source, string fileName, string expectedHeader)
    {
        var result = Controller(new DefaultHttpContext()).DownloadImportTemplate(source);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(fileName, file.FileDownloadName);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal(0xEF, file.FileContents[0]);
        Assert.Contains(expectedHeader, Encoding.UTF8.GetString(file.FileContents));
    }

    [Fact]
    public void DownloadImportTemplate_RejectsUnknownSource()
    {
        var result = Controller(new DefaultHttpContext()).DownloadImportTemplate("unknown");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ImportCsv_RequiresAtLeastOneFile()
    {
        var result = await Controller(new DefaultHttpContext()).ImportCsv(null, null, true, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ImportCsv_RejectsNonCsvFileBeforeOpeningDatabase()
    {
        var file = FormFile("bank.txt", "Dato;Tekst;Belob;Saldo\n03.09.2026;Test;1;2\n");

        var result = await Controller(new DefaultHttpContext()).ImportCsv(file, null, true, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static ApiFinanceController Controller(HttpContext context)
    {
        return new ApiFinanceController(FinanceTestHelpers.ReportService(), FinanceTestHelpers.ImportService())
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static IFormFile FormFile(string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name);
    }
}
