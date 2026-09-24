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
            return await GetOverviewCoreAsync(year, cancellationToken);
        }

        public async Task<FinanceAdminOverviewDto> GetAdminOverviewAsync(int year, CancellationToken cancellationToken)
        {
            var overview = await GetOverviewCoreAsync(year, cancellationToken);
            var today = DateTime.Today;
            var actualStart = new DateTime(year, 1, 1);
            var actualEnd = year == today.Year ? today : new DateTime(year, 12, 31);
            var previousStart = new DateTime(year - 1, 1, 1);
            var previousEnd = year == today.Year
                ? new DateTime(year - 1, today.Month, Math.Min(today.Day, DateTime.DaysInMonth(year - 1, today.Month)))
                : new DateTime(year - 1, 12, 31);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            return new FinanceAdminOverviewDto
            {
                Year = overview.Year,
                PreviousYear = overview.PreviousYear,
                IsCurrentYear = overview.IsCurrentYear,
                LastUpdated = overview.LastUpdated,
                Summary = overview.Summary,
                Monthly = overview.Monthly,
                Accounts = overview.Accounts,
                DataQuality = await ReadDataQualityAsync(connection, actualStart, actualEnd, cancellationToken),
                PostingGroups = await ReadPostingGroupsAsync(connection, actualStart, actualEnd, previousStart, previousEnd, cancellationToken),
                BankTransfers = await ReadSourceTransfersAsync(connection, actualStart, actualEnd, "Bank", cancellationToken),
                MobilePayTransfers = await ReadSourceTransfersAsync(connection, actualStart, actualEnd, "MobilePay", cancellationToken)
            };
        }

        private async Task<FinanceOverviewDto> GetOverviewCoreAsync(int year, CancellationToken cancellationToken)
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

        public async Task<FinanceAdminPostingsDto> GetAdminPostingsAsync(int year, string accountId, long? bankKey, long? mobilePayKey, CancellationToken cancellationToken)
        {
            var start = new DateTime(year, 1, 1);
            var end = year == DateTime.Today.Year ? DateTime.Today : new DateTime(year, 12, 31);
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var postings = new List<FinanceAdminPostingDto>();
            await using var command = new NpgsqlCommand(@"
                SELECT
                    p.id,
                    TO_CHAR(p.date, 'YYYY-MM-DD') AS transaction_date,
                    TO_CHAR(COALESCE(p.posterings_date, p.date), 'YYYY-MM-DD') AS posting_date,
                    COALESCE(p.text, '') AS text,
                    COALESCE(p.amount, 0) AS amount,
                    COALESCE(p.user_id, '') AS user_id,
                    COALESCE(a.main_account, '') AS account,
                    COALESCE(pg.posting_group, '') AS posting_group,
                    COALESCE(p.document, '') AS document,
                    CASE
                        WHEN p.mp_key IS NOT NULL THEN 'MobilePay'
                        WHEN p.bank_account_key IS NOT NULL THEN 'Bank'
                        ELSE 'Manuel'
                    END AS source_type,
                    CASE
                        WHEN NULLIF(TRIM(COALESCE(p.account_number, '')), '') IS NULL
                          OR NULLIF(TRIM(COALESCE(p.posting_group_id, '')), '') IS NULL THEN 'Ukategoriseret'
                        WHEN p.bank_account_key IS NOT NULL
                          AND p.mp_key IS NULL
                          AND NULLIF(TRIM(COALESCE(p.document, '')), '') IS NULL THEN 'Mangler bilag'
                        ELSE 'Bogført'
                    END AS status
                FROM public.posteringer p
                LEFT JOIN public.account a ON a.id = p.account_number
                LEFT JOIN public.postering_group pg ON pg.id = p.posting_group_id
                WHERE COALESCE(p.posterings_date, p.date) >= @start_date
                  AND COALESCE(p.posterings_date, p.date) <= @end_date
                  AND (@account_id IS NULL OR p.account_number = @account_id)
                  AND (@bank_key IS NULL OR p.bank_account_key = @bank_key)
                  AND (@mobile_pay_key IS NULL OR p.mp_key = @mobile_pay_key)
                ORDER BY COALESCE(p.posterings_date, p.date) DESC NULLS LAST, p.id DESC;", connection);

            AddDate(command, "start_date", start);
            AddDate(command, "end_date", end);
            AddNullableText(command, "account_id", accountId);
            AddNullableLong(command, "bank_key", bankKey);
            AddNullableLong(command, "mobile_pay_key", mobilePayKey);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                postings.Add(new FinanceAdminPostingDto
                {
                    Id = reader.GetString(0),
                    Date = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    PostingDate = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Text = reader.GetString(3),
                    Amount = reader.GetFieldValue<decimal>(4),
                    UserId = reader.GetString(5),
                    Account = reader.GetString(6),
                    PostingGroup = reader.GetString(7),
                    Document = reader.GetString(8),
                    SourceType = reader.GetString(9),
                    Status = reader.GetString(10)
                });
            }

            await reader.CloseAsync();
            return new FinanceAdminPostingsDto
            {
                LastUpdated = await ReadLastUpdatedAsync(connection, cancellationToken),
                Postings = postings
            };
        }

        public async Task<FinanceAdminPostingDetailDto> GetAdminPostingDetailAsync(string id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                SELECT p.id, TO_CHAR(p.date, 'YYYY-MM-DD'), TO_CHAR(COALESCE(p.posterings_date, p.date), 'YYYY-MM-DD'),
                       COALESCE(p.text, ''), COALESCE(p.amount, 0), COALESCE(p.user_id, ''), COALESCE(p.account_number, ''),
                       COALESCE(p.posting_group_id, ''), COALESCE(p.document, ''),
                       CASE WHEN p.mp_key IS NOT NULL THEN 'MobilePay' WHEN p.bank_account_key IS NOT NULL THEN 'Bank' ELSE 'Manuel' END,
                       COALESCE(a.main_account, ''), COALESCE(pg.posting_group, ''),
                       COALESCE(p.bank_account_key::text, ''), COALESCE(p.mp_key::text, ''),
                       CASE
                         WHEN NULLIF(TRIM(COALESCE(p.account_number, '')), '') IS NULL OR NULLIF(TRIM(COALESCE(p.posting_group_id, '')), '') IS NULL THEN 'Ukategoriseret'
                         WHEN p.bank_account_key IS NOT NULL AND p.mp_key IS NULL AND NULLIF(TRIM(COALESCE(p.document, '')), '') IS NULL THEN 'Mangler bilag'
                         ELSE 'Bogført'
                       END
                FROM public.posteringer p
                LEFT JOIN public.account a ON a.id = p.account_number
                LEFT JOIN public.postering_group pg ON pg.id = p.posting_group_id
                WHERE p.id = @id;", connection);
            AddText(command, "id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            return new FinanceAdminPostingDetailDto
            {
                Id = reader.GetString(0), Date = reader.IsDBNull(1) ? "" : reader.GetString(1), PostingDate = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Text = reader.GetString(3), Amount = reader.GetFieldValue<decimal>(4), UserId = reader.GetString(5), AccountId = reader.GetString(6), PostingGroupId = reader.GetString(7),
                Document = reader.GetString(8), SourceType = reader.GetString(9), Account = reader.GetString(10), PostingGroup = reader.GetString(11), BankReference = reader.GetString(12), MobilePayReference = reader.GetString(13), Status = reader.GetString(14)
            };
        }

        public async Task<FinancePostingEditorOptionsDto> GetPostingEditorOptionsAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var accounts = await ReadSelectOptionsAsync(connection, "SELECT id, CONCAT_WS(' · ', NULLIF(main_account, ''), NULLIF(sub_account, ''), NULLIF(context, '')) FROM public.account ORDER BY account_key, sub_account_key, context;", cancellationToken);
            var groups = await ReadSelectOptionsAsync(connection, "SELECT id, CONCAT_WS(' · ', NULLIF(posting_group, ''), NULLIF(context, '')) FROM public.postering_group ORDER BY posting_group, context;", cancellationToken);
            return new FinancePostingEditorOptionsDto { Accounts = accounts, PostingGroups = groups };
        }

        public async Task<bool> UpdateAdminPostingAsync(string id, FinanceAdminPostingUpdateDto update, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                UPDATE public.posteringer
                SET account_number = @account_id,
                    posting_group_id = @posting_group_id,
                    user_id = @user_id,
                    posterings_date = @posterings_date,
                    document = CASE WHEN mp_key IS NOT NULL THEN NULL ELSE @document END
                WHERE id = @id;", connection);
            AddText(command, "id", id); AddNullableText(command, "account_id", update.AccountId); AddNullableText(command, "posting_group_id", update.PostingGroupId); AddNullableText(command, "user_id", update.UserId); AddNullableDate(command, "posterings_date", update.PostingDate); AddNullableText(command, "document", update.Document);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }

        private static async Task<IReadOnlyList<FinanceSelectOptionDto>> ReadSelectOptionsAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
        {
            var result = new List<FinanceSelectOptionDto>();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) result.Add(new FinanceSelectOptionDto { Id = reader.GetString(0), Label = reader.IsDBNull(1) ? reader.GetString(0) : reader.GetString(1) });
            return result;
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
                    a.context,
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
                GROUP BY a.main_account, a.account_key, a.sub_account, a.sub_account_key, a.context, a.context_key
                ORDER BY a.account_key NULLS LAST, a.sub_account_key NULLS LAST, a.context_key NULLS LAST, MIN(a.id);", connection);

            AddInteger(command, "year", year);
            AddDate(command, "actual_start", actualStart);
            AddDate(command, "actual_end", actualEnd);
            AddDate(command, "previous_start", previousStart);
            AddDate(command, "previous_end", previousEnd);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var contextLabel = reader.IsDBNull(5) ? "" : reader.GetString(5);
                var contextKey = (int)reader.GetInt64(6);
                result.Add(new FinanceAccountRowDto
                {
                    AccountId = reader.GetString(0),
                    MainAccount = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    AccountKey = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                    SubAccount = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    SubAccountKey = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    Context = string.IsNullOrWhiteSpace(contextLabel) ? (contextKey == 1 ? "Indtægt" : "Udgift") : contextLabel,
                    ContextKey = contextKey,
                    Realized = reader.GetFieldValue<decimal>(7),
                    PreviousYear = reader.GetFieldValue<decimal>(8),
                    Budget = reader.GetFieldValue<decimal>(9)
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

        private static async Task<IReadOnlyList<FinancePostingGroupRowDto>> ReadPostingGroupsAsync(NpgsqlConnection connection, DateTime start, DateTime end, DateTime previousStart, DateTime previousEnd, CancellationToken cancellationToken)
        {
            var result = new List<FinancePostingGroupRowDto>();
            await using var command = new NpgsqlCommand(@"
                SELECT
                    COALESCE(pg.id, ''),
                    COALESCE(NULLIF(pg.posting_group, ''), 'Ukategoriseret'),
                    COALESCE(pg.context, ''),
                    COUNT(*) FILTER (WHERE p.posterings_date >= @start_date AND p.posterings_date <= @end_date)::int,
                    COALESCE(SUM(CASE WHEN p.posterings_date >= @start_date AND p.posterings_date <= @end_date THEN p.amount ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN p.posterings_date >= @previous_start AND p.posterings_date <= @previous_end THEN p.amount ELSE 0 END), 0)
                FROM public.posteringer p
                LEFT JOIN public.postering_group pg ON pg.id = p.posting_group_id
                WHERE p.posterings_date >= @previous_start
                  AND p.posterings_date <= @end_date
                GROUP BY pg.id, pg.posting_group, pg.context
                ORDER BY COALESCE(pg.context, ''), COALESCE(pg.posting_group, 'Ukategoriseret');", connection);
            AddDate(command, "start_date", start);
            AddDate(command, "end_date", end);
            AddDate(command, "previous_start", previousStart);
            AddDate(command, "previous_end", previousEnd);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new FinancePostingGroupRowDto
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Context = reader.GetString(2),
                    PostingCount = reader.GetInt32(3),
                    Amount = reader.GetFieldValue<decimal>(4),
                    PreviousAmount = reader.GetFieldValue<decimal>(5)
                });
            }
            return result;
        }

        private static async Task<IReadOnlyList<FinanceSourcePostingDto>> ReadSourceTransfersAsync(NpgsqlConnection connection, DateTime start, DateTime end, string sourceType, CancellationToken cancellationToken)
        {
            var result = new List<FinanceSourcePostingDto>();
            var isBank = sourceType == "Bank";
            var sql = isBank
                ? @"
                    SELECT
                        b.id,
                        TO_CHAR(b.date, 'YYYY-MM-DD'),
                        COALESCE(b.text, ''),
                        COALESCE(b.amount, 0)
                    FROM public.bank_account b
                    WHERE b.date >= @start_date AND b.date <= @end_date
                    ORDER BY b.date DESC NULLS LAST, b.id DESC;"
                : @"
                    SELECT
                        m.id,
                        TO_CHAR(m.date, 'YYYY-MM-DD'),
                        COALESCE(
                            NULLIF(CONCAT_WS(' · ', NULLIF(m.payner_name, ''), NULLIF(m.message, '')), ''),
                            m.transaction_type,
                            ''
                        ),
                        COALESCE(m.amount, 0)
                    FROM public.mobilepay m
                    WHERE m.date >= @start_date AND m.date <= @end_date
                    ORDER BY m.date DESC NULLS LAST, m.id DESC;";
            await using var command = new NpgsqlCommand(sql, connection);
            AddDate(command, "start_date", start);
            AddDate(command, "end_date", end);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var sourceId = reader.GetInt64(0);
                result.Add(new FinanceSourcePostingDto
                {
                    Id = $"{(isBank ? "BA" : "MP")}-{sourceId}",
                    Date = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Text = reader.GetString(2),
                    Amount = reader.GetFieldValue<decimal>(3),
                    SourceId = sourceId,
                    SourceType = sourceType
                });
            }
            return result;
        }

        private static async Task<FinanceDataQualityDto> ReadDataQualityAsync(NpgsqlConnection connection, DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                SELECT
                    COUNT(*)::int,
                    COUNT(*) FILTER (
                        WHERE NULLIF(TRIM(COALESCE(p.account_number, '')), '') IS NOT NULL
                          AND NULLIF(TRIM(COALESCE(p.posting_group_id, '')), '') IS NOT NULL
                    )::int
                FROM public.posteringer p
                WHERE COALESCE(p.posterings_date, p.date) >= @start_date
                  AND COALESCE(p.posterings_date, p.date) <= @end_date;", connection);

            AddDate(command, "start_date", start);
            AddDate(command, "end_date", end);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            var total = reader.GetInt32(0);
            var categorized = reader.GetInt32(1);
            return new FinanceDataQualityDto
            {
                TotalPostings = total,
                CategorizedPostings = categorized,
                Percentage = total == 0 ? 0 : Math.Round(categorized * 100m / total, 1)
            };
        }

        private static void AddText(NpgsqlCommand command, string name, string value) => command.Parameters.Add(name, NpgsqlDbType.Text).Value = value;
        private static void AddNullableText(NpgsqlCommand command, string name, string value) => command.Parameters.Add(name, NpgsqlDbType.Text).Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
        private static void AddNullableLong(NpgsqlCommand command, string name, long? value) => command.Parameters.Add(name, NpgsqlDbType.Bigint).Value = value ?? (object)DBNull.Value;
        private static void AddNullableDate(NpgsqlCommand command, string name, string value)
        {
            var parameter = command.Parameters.Add(name, NpgsqlDbType.Date);
            parameter.Value = DateTime.TryParse(value, out var parsed) ? parsed.Date : DBNull.Value;
        }
        private static void AddInteger(NpgsqlCommand command, string name, int value) => command.Parameters.Add(name, NpgsqlDbType.Integer).Value = value;
        private static void AddDate(NpgsqlCommand command, string name, DateTime value) => command.Parameters.Add(name, NpgsqlDbType.Date).Value = value.Date;
    }

    public class FinanceOverviewDto
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

    public sealed class FinanceAdminOverviewDto : FinanceOverviewDto
    {
        public FinanceDataQualityDto DataQuality { get; set; } = new();
        public IReadOnlyList<FinancePostingGroupRowDto> PostingGroups { get; set; } = Array.Empty<FinancePostingGroupRowDto>();
        public IReadOnlyList<FinanceSourcePostingDto> BankTransfers { get; set; } = Array.Empty<FinanceSourcePostingDto>();
        public IReadOnlyList<FinanceSourcePostingDto> MobilePayTransfers { get; set; } = Array.Empty<FinanceSourcePostingDto>();
    }

    public sealed class FinanceDataQualityDto
    {
        public int TotalPostings { get; set; }
        public int CategorizedPostings { get; set; }
        public decimal Percentage { get; set; }
    }

    public sealed class FinancePostingGroupRowDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Context { get; set; } = "";
        public int PostingCount { get; set; }
        public decimal Amount { get; set; }
        public decimal PreviousAmount { get; set; }
    }

    public sealed class FinanceSourcePostingDto
    {
        public string Id { get; set; } = "";
        public string Date { get; set; } = "";
        public string Text { get; set; } = "";
        public decimal Amount { get; set; }
        public long SourceId { get; set; }
        public string SourceType { get; set; } = "";
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

    public sealed class FinanceAdminPostingDto
    {
        public string Id { get; set; } = "";
        public string Date { get; set; } = "";
        public string PostingDate { get; set; } = "";
        public string Text { get; set; } = "";
        public decimal Amount { get; set; }
        public string UserId { get; set; } = "";
        public string Account { get; set; } = "";
        public string PostingGroup { get; set; } = "";
        public string Document { get; set; } = "";
        public string SourceType { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public sealed class FinanceAdminPostingsDto
    {
        public string LastUpdated { get; set; }
        public IReadOnlyList<FinanceAdminPostingDto> Postings { get; set; } = Array.Empty<FinanceAdminPostingDto>();
    }

    public sealed class FinanceAdminPostingDetailDto
    {
        public string Id { get; set; } = ""; public string Date { get; set; } = ""; public string PostingDate { get; set; } = ""; public string Text { get; set; } = ""; public decimal Amount { get; set; }
        public string UserId { get; set; } = ""; public string AccountId { get; set; } = ""; public string PostingGroupId { get; set; } = ""; public string Document { get; set; } = ""; public string SourceType { get; set; } = ""; public string Account { get; set; } = ""; public string PostingGroup { get; set; } = ""; public string BankReference { get; set; } = ""; public string MobilePayReference { get; set; } = ""; public string Status { get; set; } = "";
    }

    public sealed class FinanceSelectOptionDto { public string Id { get; set; } = ""; public string Label { get; set; } = ""; }
    public sealed class FinancePostingEditorOptionsDto { public IReadOnlyList<FinanceSelectOptionDto> Accounts { get; set; } = Array.Empty<FinanceSelectOptionDto>(); public IReadOnlyList<FinanceSelectOptionDto> PostingGroups { get; set; } = Array.Empty<FinanceSelectOptionDto>(); }
    public sealed class FinanceAdminPostingUpdateDto { public string AccountId { get; set; } = ""; public string PostingGroupId { get; set; } = ""; public string UserId { get; set; } = ""; public string PostingDate { get; set; } = ""; public string Document { get; set; } = ""; }
}
