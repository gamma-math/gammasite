using System.Collections.Generic;
using GamMaSite.Services;
using Microsoft.Extensions.Configuration;

namespace GamMaSite.Tests;

internal static class FinanceTestHelpers
{
    public static IConfiguration Configuration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FinanceRead:Host"] = "localhost",
                ["FinanceRead:Port"] = "5433",
                ["FinanceRead:Database"] = "finance-tests",
                ["FinanceRead:Username"] = "test-read",
                ["FinanceRead:Password"] = "test-read-password",
                ["FinanceWrite:Host"] = "localhost",
                ["FinanceWrite:Port"] = "5433",
                ["FinanceWrite:Database"] = "finance-tests",
                ["FinanceWrite:Username"] = "test-write",
                ["FinanceWrite:Password"] = "test-write-password"
            })
            .Build();
    }

    public static FinanceReportService ReportService() => new(Configuration());

    public static FinanceImportService ImportService() => new(Configuration());
}
