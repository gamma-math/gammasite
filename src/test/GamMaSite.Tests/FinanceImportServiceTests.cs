using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GamMaSite.Tests;

public sealed class FinanceImportServiceTests
{
    [Fact]
    public async Task Import_RejectsMissingFilesBeforeOpeningDatabase()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ImportService().ImportAsync(null, null, null, null, true, CancellationToken.None));

        Assert.Contains("CSV", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_RejectsInvalidBankAmountBeforeOpeningDatabase()
    {
        using var bank = Csv("Dato;Tekst;Belob;Saldo\n03.09.2026;Test;not-money;100\n");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ImportService().ImportAsync(bank, "bank.csv", null, null, true, CancellationToken.None));

        Assert.Contains("bel", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2", exception.Message);
    }

    [Fact]
    public async Task Import_RejectsMissingMobilePayRequiredColumnValueBeforeOpeningDatabase()
    {
        using var mobilePay = Csv("Date;Timestamp;Amount;Message;Transaction Type;Transfer Reference;Transfer Date;Payment Transaction ID;User Name\n02-09-2026;2026-09-02T18:28:10+02:00;50,00;Payment;Payment;REF-1;03-09-2026;;Example\n");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ImportService().ImportAsync(null, null, mobilePay, "mobilepay.csv", false, CancellationToken.None));

        Assert.Contains("Payment Transaction ID", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_RejectsMissingMobilePayMessageAndTransferDateBeforeOpeningDatabase()
    {
        using var mobilePay = Csv("Date;Timestamp;Amount;Message;Transaction Type;Transfer Reference;Transfer Date;Payment Transaction ID;User Name\n02-09-2026;2026-09-02T18:28:10+02:00;50,00;;Payment;REF-1;;TX-1;Example\n");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            FinanceTestHelpers.ImportService().ImportAsync(null, null, mobilePay, "mobilepay.csv", false, CancellationToken.None));

        Assert.Contains("Message", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_ParsesSemicolonAndDanishDecimalBeforeOpeningDatabase()
    {
        using var bank = Csv("Dato;Tekst;Beløb;Saldo\n03.09.2026;Test;-244,00;12.500,50\n");

        // Parsing succeeds; the test then reaches the database boundary. A
        // connection failure proves no parsing/validation error was raised.
        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            FinanceTestHelpers.ImportService().ImportAsync(bank, "bank.csv", null, null, false, CancellationToken.None));

        Assert.DoesNotContain("CSV-række", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MemoryStream Csv(string content) => new(Encoding.UTF8.GetBytes(content));
}
