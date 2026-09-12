using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace GamMaSite.Services
{
    public sealed class FinanceReportService
    {
        private readonly string _connectionString;

        public FinanceReportService(IConfiguration configuration)
        {
            var host = configuration["Finance:Host"];
            var database = configuration["Finance:Database"];
            var username = configuration["Finance:Username"];
            var password = configuration["Finance:Password"];
            var portValue = configuration["Finance:Port"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Finance-databasekonfigurationen mangler. Kontrollér .env.local.");
            }

            _connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(portValue, out var port) ? port : 5432,
                Database = database,
                Username = username,
                Password = password
            }.ConnectionString;
        }

        public async Task<FinanceOverviewDto> GetOverviewAsync(string userId, int year, CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var isCurrentYear = year == today.Year;
            var actualStart = new DateTime(year, 1, 1);
            var actualEnd = isCurrentYear ? today : new DateTime(year, 12, 31);
            var previousStart = new DateTime(year - 1, 1, 1);
            var previousEnd = isCurrentYear
                ? new DateTime(year - 1, today.Month, Math.Min(today.Day, DateTime.DaysInMonth(year - 1, today.Month)))
                : new DateTime(year - 1, 12, 31);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var summary = await ReadSummaryAsync(connection, actualStart, actualEnd, cancellationToken);
            var monthly = await ReadMonthlyAsync(connection, actualStart, actualEnd, cancellationToken);
            var accounts = await ReadAccountsAsync(connection, year, actualStart, actualEnd, previousStart, previousEnd, cancellationToken);
            var lastUpdated = await ReadLastUpdatedAsync(connection, cancellationToken);

            return new FinanceOverviewDto
            {
                Year = year,
                PreviousYear = year - 1,
                IsCurrentYear = isCurrentYear,
                Summary = summary,
                Monthly = monthly,
                Accounts = accounts,
                LastUpdated = lastUpdated
            };
        }

        public async Task<FinanceUserPostingsDto> GetUserPostingsAsync(string userId, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var postings = new List<FinancePostingDto>();
            await using var command = new NpgsqlCommand(@"
                SELECT
                    p.id,
                    TO_CHAR(COALESCE(p.posterings_date, p.date), 'YYYY-MM-DD') AS posting_date,
                    COALESCE(p.amount, 0) AS amount,
                    COALESCE(p.text, '') AS text,
                    CASE
                        WHEN p.mp_key IS NOT NULL THEN 'MobilePay'
                        WHEN p.bank_account_key IS NOT NULL THEN 'Bank'
                        ELSE 'Manuel'
                    END AS source_type,
                    COALESCE(a.main_account, '') AS account,
                    COALESCE(pg.posting_group, '') AS posting_group
                FROM public.posteringer p
                LEFT JOIN public.account a ON a.id = p.account_number
                LEFT JOIN public.postering_group pg ON pg.id = p.posting_group_id
                WHERE p.user_id = @user_id
                ORDER BY COALESCE(p.posterings_date, p.date) DESC NULLS LAST, p.id DESC;", connection);

            AddText(command, "user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                postings.Add(new FinancePostingDto
                {
                    Id = reader.GetString(0),
                    Date = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Amount = reader.GetFieldValue<decimal>(2),
                    Text = reader.GetString(3),
                    SourceType = reader.GetString(4),
                    Account = reader.GetString(5),
                    PostingGroup = reader.GetString(6)
                });
            }

            await reader.CloseAsync();
            var lastUpdated = await ReadLastUpdatedAsync(connection, cancellationToken);

            return new FinanceUserPostingsDto
            {
                LastUpdated = lastUpdated,
                Postings = postings
            };
        }

        private static async Task<FinanceSummaryDto> ReadSummaryAsync(NpgsqlConnection connection, DateTime actualStart, DateTime actualEnd, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                SELECT
                    COALESCE(SUM(CASE WHEN a.context_key = 1 THEN p.amount ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN a.context_key = 2 THEN p.amount ELSE 0 END), 0),
                    COALESCE(SUM(p.amount), 0)
                FROM public.posteringer p
                LEFT JOIN public.account a ON a.id = p.account_number
                WHERE p.posterings_date >= @actual_start
                  AND p.posterings_date <= @actual_end;", connection);

            AddDate(command, "actual_start", actualStart);
            AddDate(command, "actual_end", actualEnd);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new FinanceSummaryDto
            {
                Income = reader.GetFieldValue<decimal>(0),
                Expense = reader.GetFieldValue<decimal>(1),
                Net = reader.GetFieldValue<decimal>(2)
            };
        }

        private static async Task<IReadOnlyList<FinanceMonthDto>> ReadMonthlyAsync(NpgsqlConnection connection, DateTime actualStart, DateTime actualEnd, CancellationToken cancellationToken)
        {
            var values = new decimal[12];
            await using var command = new NpgsqlCommand(@"
                SELECT EXTRACT(MONTH FROM p.posterings_date)::int AS month_number,
                       COALESCE(SUM(p.amount), 0) AS net
                FROM public.posteringer p
                WHERE p.posterings_date >= @actual_start
                  AND p.posterings_date <= @actual_end
                GROUP BY month_number
                ORDER BY month_number;", connection);

            AddDate(command, "actual_start", actualStart);
            AddDate(command, "actual_end", actualEnd);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var month = reader.GetInt32(0);
                if (month is >= 1 and <= 12)
                {
                    values[month - 1] = reader.GetFieldValue<decimal>(1);
                }
            }

            var result = new List<FinanceMonthDto>(12);
            for (var index = 0; index < values.Length; index++)
            {
                result.Add(new FinanceMonthDto { Month = index + 1, Net = values[index] });
            }

            return result;
        }

        private static async Task<IReadOnlyList<FinanceAccountRowDto>> ReadAccountsAsync(
            NpgsqlConnection connection,
            int year,
            DateTime actualStart,
            DateTime actualEnd,
            DateTime previousStart,
            DateTime previousEnd,
            CancellationToken cancellationToken)
        {
            var result = new List<FinanceAccountRowDto>();
            await using var command = new NpgsqlCommand(@"
                SELECT
                    MIN(a.id) AS account_id,
                    a.main_account,
                    a.account_key,
                    a.sub_account,
                    a.sub_account_key,
                    a.context_key,
                    COALESCE(SUM(CASE
                        WHEN p.posterings_date >= @actual_start AND p.posterings_date <= @actual_end
                        THEN p.amount ELSE 0 END), 0) AS realized,
                    COALESCE(SUM(CASE
                        WHEN p.posterings_date >= @previous_start AND p.posterings_date <= @previous_end
                        THEN p.amount ELSE 0 END), 0) AS previous_year,
                    COALESCE((
                        SELECT SUM(f.forecast)
                        FROM public.forecast f
                        INNER JOIN public.account forecast_account ON forecast_account.id = f.account_id
                        WHERE forecast_account.account_key IS NOT DISTINCT FROM a.account_key
                          AND forecast_account.sub_account_key IS NOT DISTINCT FROM a.sub_account_key
                          AND forecast_account.context_key IS NOT DISTINCT FROM a.context_key
                          AND f.year_actual = @year
                          AND f.forecast_type = 'BU'
                    ), 0) AS budget
                FROM public.account a
                LEFT JOIN public.posteringer p
                    ON p.account_number = a.id
                   AND p.posterings_date IS NOT NULL
                WHERE a.context_key IN (1, 2)
                GROUP BY a.main_account, a.account_key, a.sub_account, a.sub_account_key, a.context_key
                ORDER BY a.account_key NULLS LAST, a.sub_account_key NULLS LAST, a.context_key NULLS LAST, MIN(a.id);", connection);

            AddInteger(command, "year", year);
            AddDate(command, "actual_start", actualStart);
            AddDate(command, "actual_end", actualEnd);
            AddDate(command, "previous_start", previousStart);
            AddDate(command, "previous_end", previousEnd);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var contextKey = (int)reader.GetInt64(5);
                result.Add(new FinanceAccountRowDto
                {
                    AccountId = reader.GetString(0),
                    MainAccount = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    AccountKey = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                    SubAccount = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    SubAccountKey = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    Context = contextKey == 1 ? "Indtægt" : "Udgift",
                    ContextKey = contextKey,
                    Realized = reader.GetFieldValue<decimal>(6),
                    PreviousYear = reader.GetFieldValue<decimal>(7),
                    Budget = reader.GetFieldValue<decimal>(8)
                });
            }

            return result;
        }

        private static async Task<string> ReadLastUpdatedAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                SELECT TO_CHAR(MAX(imported_at AT TIME ZONE 'Europe/Copenhagen'), 'YYYY-MM-DD HH24:MI:SS')
                FROM public.import_history
                WHERE status = 'completed';", connection);

            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value == DBNull.Value || value == null ? null : value.ToString();
        }

        private static void AddText(NpgsqlCommand command, string name, string value) => command.Parameters.Add(name, NpgsqlDbType.Text).Value = value;
        private static void AddInteger(NpgsqlCommand command, string name, int value) => command.Parameters.Add(name, NpgsqlDbType.Integer).Value = value;
        private static void AddDate(NpgsqlCommand command, string name, DateTime value) => command.Parameters.Add(name, NpgsqlDbType.Date).Value = value.Date;
    }

    public sealed class FinanceOverviewDto
    {
        public int Year { get; set; }
        public int PreviousYear { get; set; }
        public bool IsCurrentYear { get; set; }
        public string LastUpdated { get; set; }
        public FinanceSummaryDto Summary { get; set; } = new();
        public IReadOnlyList<FinanceMonthDto> Monthly { get; set; } = Array.Empty<FinanceMonthDto>();
        public IReadOnlyList<FinanceAccountRowDto> Accounts { get; set; } = Array.Empty<FinanceAccountRowDto>();
    }

    public sealed class FinanceSummaryDto
    {
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Net { get; set; }
    }

    public sealed class FinanceMonthDto
    {
        public int Month { get; set; }
        public decimal Net { get; set; }
    }

    public sealed class FinanceAccountRowDto
    {
        public string AccountId { get; set; } = "";
        public string MainAccount { get; set; } = "";
        public long? AccountKey { get; set; }
        public string SubAccount { get; set; } = "";
        public long? SubAccountKey { get; set; }
        public string Context { get; set; } = "";
        public int ContextKey { get; set; }
        public decimal Realized { get; set; }
        public decimal Budget { get; set; }
        public decimal PreviousYear { get; set; }
    }

    public sealed class FinancePostingDto
    {
        public string Id { get; set; } = "";
        public string Date { get; set; } = "";
        public decimal Amount { get; set; }
        public string Text { get; set; } = "";
        public string SourceType { get; set; } = "";
        public string Account { get; set; } = "";
        public string PostingGroup { get; set; } = "";
    }

    public sealed class FinanceUserPostingsDto
    {
        public string LastUpdated { get; set; }
        public IReadOnlyList<FinancePostingDto> Postings { get; set; } = Array.Empty<FinancePostingDto>();
    }
}
