using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GamMaSite.Tests;

public sealed class FinanceReportServiceTests
{
    [Fact]
    public void FinanceServices_AcceptDotNetPostgreSqlConnectionStrings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FinanceRead:CONNECTION_STRING"] = "Host=ep-example-pooler.eu-central-1.aws.neon.tech;Database=neondb;Username=read_user;Password=\"p@ss'word\";SSL Mode=VerifyFull;Channel Binding=Require",
                ["FinanceWrite:CONNECTION_STRING"] = "Host=ep-example-pooler.eu-central-1.aws.neon.tech;Database=neondb;Username=write_user;Password=\"p@ss'word\";SSL Mode=VerifyFull;Channel Binding=Require"
            })
            .Build();

        _ = new FinanceReportService(configuration);
        _ = new FinanceImportService(configuration);
    }

    [Fact]
    public async Task CreateBudget_RejectsMissingIdBeforeOpeningDatabase()
    {
        var update = new FinanceBudgetUpdateDto { AccountId = "A-1", YearActual = 2026 };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().CreateAdminBudgetAsync(update, CancellationToken.None));

        Assert.Contains("ID", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBudget_RejectsMissingAccountBeforeOpeningDatabase()
    {
        var update = new FinanceBudgetUpdateDto { Id = "2026-A-1", YearActual = 2026 };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().CreateAdminBudgetAsync(update, CancellationToken.None));

        Assert.Contains("Account", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBudget_RejectsInvalidYearBeforeOpeningDatabase()
    {
        var update = new FinanceBudgetUpdateDto { Id = "2026-A-1", AccountId = "A-1", YearActual = 0 };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().CreateAdminBudgetAsync(update, CancellationToken.None));

        Assert.Contains("gyldigt", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportBudgetCsv_RejectsEmptyIdBeforeOpeningDatabase()
    {
        using var csv = Csv("id;account number;account;postinggroup id;postinggroup;year;amount;budgettype\n;A-1;Test;Tom;Tom;2026;20000;BU\n");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().ImportAdminBudgetsAsync(csv, CancellationToken.None));

        Assert.Contains("ID", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportBudgetCsv_RejectsDuplicateIdsBeforeOpeningDatabase()
    {
        using var csv = Csv(
            "id;account number;account;postinggroup id;postinggroup;year;amount;budgettype\n" +
            "2026-A-1;A-1;Test;Tom;Tom;2026;20000;BU\n" +
            "2026-A-1;A-1;Test;Tom;Tom;2026;20000;BU\n");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().ImportAdminBudgetsAsync(csv, CancellationToken.None));

        Assert.Contains("flere gange", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportBudgetCsv_AcceptsWindows1252DanishCharacters()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var windows1252 = Encoding.GetEncoding(1252);
        using var csv = new MemoryStream(windows1252.GetBytes(
            "id;account number;account;postinggroup id;postinggroup;year;amount;budgettype\n" +
            ";A-1;Arrangement - Deltagerbetaling - Indtægt;Tom;Tom;2026;20000;BU\n"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().ImportAdminBudgetsAsync(csv, CancellationToken.None));

        Assert.Contains("ID", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MemoryStream Csv(string content) =>
        new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task CreatePostingGroup_RejectsMissingIdBeforeOpeningDatabase()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().CreateAdminPostingGroupAsync(
                new FinancePostingGroupUpdateDto(), CancellationToken.None));

        Assert.Contains("ID", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePosting_RejectsMissingIdBeforeOpeningDatabase()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().CreateAdminPostingAsync(
                new FinanceAdminPostingUpdateDto { Amount = 10m }, CancellationToken.None));

        Assert.Contains("ID", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePosting_RejectsInvalidAmountBeforeOpeningDatabase()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ReportService().CreateAdminPostingAsync(
                new FinanceAdminPostingUpdateDto { Id = "P-1", Amount = decimal.MaxValue }, CancellationToken.None));

        Assert.Contains("bel", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
