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
                ["FinanceRead:CONNECTION_STRING"] = "Host=localhost;Port=5433;Database=finance-tests;Username=test-read;Password=test-read-password",
                ["FinanceWrite:CONNECTION_STRING"] = "Host=localhost;Port=5433;Database=finance-tests;Username=test-write;Password=test-write-password"
            })
            .Build();
    }

    public static FinanceReportService ReportService() => new(Configuration());

    public static FinanceImportService ImportService() => new(Configuration());
}
