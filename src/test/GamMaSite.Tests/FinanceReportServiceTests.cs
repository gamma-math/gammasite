using System;
using System.Threading;
using System.Threading.Tasks;
using GamMaSite.Services;
using Xunit;

namespace GamMaSite.Tests;

public sealed class FinanceReportServiceTests
{
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
