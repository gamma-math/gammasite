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
        private readonly string _writeConnectionString;

        public FinanceReportService(IConfiguration configuration)
        {
            var host = configuration["FinanceRead:Host"] ?? configuration["Finance:Host"];
            var database = configuration["FinanceRead:Database"] ?? configuration["Finance:Database"];
            var username = configuration["FinanceRead:Username"] ?? configuration["Finance:Username"];
            var password = configuration["FinanceRead:Password"] ?? configuration["Finance:Password"];
            var portValue = configuration["FinanceRead:Port"] ?? configuration["Finance:Port"];

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
                Password = password,
                ApplicationName = "GamMaSite Finance Read"
            }.ConnectionString;

            var writeHost = configuration["FinanceWrite:Host"] ?? configuration["Finance:Host"];
            var writeDatabase = configuration["FinanceWrite:Database"] ?? configuration["Finance:Database"];
            var writeUsername = configuration["FinanceWrite:Username"] ?? configuration["Finance:Username"];
            var writePassword = configuration["FinanceWrite:Password"] ?? configuration["Finance:Password"];
            var writePortValue = configuration["FinanceWrite:Port"] ?? configuration["Finance:Port"];

            if (string.IsNullOrWhiteSpace(writeHost) || string.IsNullOrWhiteSpace(writeDatabase) || string.IsNullOrWhiteSpace(writeUsername) || string.IsNullOrWhiteSpace(writePassword))
            {
                throw new InvalidOperationException("Finance-skriveadgangen mangler. KontrollÃ©r .env.local.");
            }

            _writeConnectionString = new NpgsqlConnectionStringBuilder
            {
                Host = writeHost,
                Port = int.TryParse(writePortValue, out var writePort) ? writePort : 5432,
                Database = writeDatabase,
                Username = writeUsername,
                Password = writePassword,
                ApplicationName = "GamMaSite Finance Write"
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

        public async Task<FinanceAdminBudgetsDto> GetAdminBudgetsAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return new FinanceAdminBudgetsDto
            {
                Budgets = await ReadBudgetsAsync(connection, cancellationToken),
                Accounts = await ReadSelectOptionsAsync(connection, "SELECT id, CONCAT_WS(' · ', NULLIF(main_account, ''), NULLIF(sub_account, ''), NULLIF(context, '')) FROM public.account ORDER BY account_key, sub_account_key, context;", cancellationToken),
                PostingGroups = await ReadSelectOptionsAsync(connection, "SELECT id, CONCAT_WS(' · ', NULLIF(posting_group, ''), NULLIF(context, '')) FROM public.postering_group ORDER BY posting_group, context;", cancellationToken)
            };
        }

        public async Task<IReadOnlyList<FinanceAccountPlanDto>> GetAdminAccountsAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return await ReadAccountPlanAsync(connection, cancellationToken);
        }

        public async Task<FinanceBudgetDto> GetAdminBudgetAsync(string id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return await ReadBudgetAsync(connection, id, cancellationToken);
        }

        public async Task<FinanceBudgetDto> CreateAdminBudgetAsync(FinanceBudgetUpdateDto update, CancellationToken cancellationToken)
        {
            ValidateBudget(update);
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureBudgetReferencesAsync(connection, update, cancellationToken);
            await using var command = new NpgsqlCommand(@"
                INSERT INTO public.forecast (id, account_id, postering_group_id, year_actual, forecast, forecast_type, created_at, updated_at)
                VALUES (@id, @account_id, @posting_group_id, @year_actual, @forecast, @forecast_type, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
            AddText(command, "id", update.Id.Trim());
            AddText(command, "account_id", update.AccountId.Trim());
            AddNullableText(command, "posting_group_id", update.PostingGroupId);
            AddInteger(command, "year_actual", update.YearActual);
            AddNumeric(command, "forecast", update.Forecast);
            AddNullableText(command, "forecast_type", update.ForecastType);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return await ReadBudgetAsync(connection, update.Id.Trim(), cancellationToken);
        }

        public async Task<FinanceBudgetDto> UpdateAdminBudgetAsync(string originalId, FinanceBudgetUpdateDto update, CancellationToken cancellationToken)
        {
            ValidateBudget(update);
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await EnsureBudgetReferencesAsync(connection, update, cancellationToken);
            await using var command = new NpgsqlCommand(@"
                UPDATE public.forecast
                SET id = @new_id,
                    account_id = @account_id,
                    postering_group_id = @posting_group_id,
                    year_actual = @year_actual,
                    forecast = @forecast,
                    forecast_type = @forecast_type,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @original_id;", connection);
            AddText(command, "original_id", originalId);
            AddText(command, "new_id", update.Id.Trim());
            AddText(command, "account_id", update.AccountId.Trim());
            AddNullableText(command, "posting_group_id", update.PostingGroupId);
            AddInteger(command, "year_actual", update.YearActual);
            AddNumeric(command, "forecast", update.Forecast);
            AddNullableText(command, "forecast_type", update.ForecastType);
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1) return null;
            return await ReadBudgetAsync(connection, update.Id.Trim(), cancellationToken);
        }

        public async Task<bool> DeleteAdminBudgetAsync(string id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM public.forecast WHERE id = @id;", connection);
            AddText(command, "id", id);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }

        public async Task<IReadOnlyList<FinancePostingGroupDto>> GetAdminPostingGroupsAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return await ReadPostingGroupDefinitionsAsync(connection, cancellationToken);
        }

        public async Task<FinancePostingGroupDto> GetAdminPostingGroupAsync(string id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT id, COALESCE(posting_group, ''), COALESCE(context, '') FROM public.postering_group WHERE id = @id;", connection);
            AddText(command, "id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new FinancePostingGroupDto { Id = reader.GetString(0), PostingGroup = reader.GetString(1), Context = reader.GetString(2) }
                : null;
        }

        public async Task<FinancePostingGroupDto> CreateAdminPostingGroupAsync(FinancePostingGroupUpdateDto update, CancellationToken cancellationToken)
        {
            ValidatePostingGroup(update);
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("INSERT INTO public.postering_group (id, posting_group, context, created_at, updated_at) VALUES (@id, @posting_group, @context, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
            AddText(command, "id", update.Id.Trim());
            AddNullableText(command, "posting_group", update.PostingGroup);
            AddNullableText(command, "context", update.Context);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return await GetAdminPostingGroupAsync(update.Id.Trim(), cancellationToken);
        }

        public async Task<FinancePostingGroupDto> UpdateAdminPostingGroupAsync(string originalId, FinancePostingGroupUpdateDto update, CancellationToken cancellationToken)
        {
            ValidatePostingGroup(update);
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                UPDATE public.postering_group
                SET id = @new_id, posting_group = @posting_group, context = @context, updated_at = CURRENT_TIMESTAMP
                WHERE id = @original_id;", connection);
            AddText(command, "original_id", originalId);
            AddText(command, "new_id", update.Id.Trim());
            AddNullableText(command, "posting_group", update.PostingGroup);
            AddNullableText(command, "context", update.Context);
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1) return null;
            return await GetAdminPostingGroupAsync(update.Id.Trim(), cancellationToken);
        }

        public async Task<bool> DeleteAdminPostingGroupAsync(string id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM public.postering_group WHERE id = @id;", connection);
            AddText(command, "id", id);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
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

        public async Task<FinanceAdminPostingsDto> GetAdminPostingsAsync(int? year, string accountId, long? bankKey, long? mobilePayKey, CancellationToken cancellationToken)
        {
            DateTime? start = year.HasValue ? new DateTime(year.Value, 1, 1) : null;
            DateTime? end = !year.HasValue
                ? null
                : year.Value == DateTime.Today.Year
                    ? DateTime.Today
                    : new DateTime(year.Value, 12, 31);
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
                    COALESCE(p.account_number, '') AS account_id,
                    CONCAT_WS(' · ', NULLIF(a.main_account, ''), NULLIF(a.sub_account, ''), NULLIF(a.context, '')) AS account,
                    COALESCE(p.posting_group_id, '') AS posting_group_id,
                    CONCAT_WS(' · ', NULLIF(pg.posting_group, ''), NULLIF(pg.context, '')) AS posting_group,
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
                WHERE (@start_date IS NULL OR COALESCE(p.posterings_date, p.date) >= @start_date)
                  AND (@end_date IS NULL OR COALESCE(p.posterings_date, p.date) <= @end_date)
                  AND (@account_id IS NULL OR p.account_number = @account_id)
                  AND (@bank_key IS NULL OR p.bank_account_key = @bank_key)
                  AND (@mobile_pay_key IS NULL OR p.mp_key = @mobile_pay_key)
                ORDER BY COALESCE(p.posterings_date, p.date) DESC NULLS LAST, p.id DESC;", connection);

            AddNullableDate(command, "start_date", start);
            AddNullableDate(command, "end_date", end);
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
                    AccountId = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Account = reader.GetString(7),
                    PostingGroupId = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    PostingGroup = reader.GetString(9),
                    Document = reader.GetString(10),
                    SourceType = reader.GetString(11),
                    Status = reader.GetString(12)
                });
            }

            await reader.CloseAsync();
            return new FinanceAdminPostingsDto
            {
                LastUpdated = await ReadLastUpdatedAsync(connection, cancellationToken),
                Postings = postings
            };
        }

        public async Task<IReadOnlyList<int>> GetAdminPostingYearsAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                SELECT DISTINCT EXTRACT(YEAR FROM COALESCE(p.posterings_date, p.posting_date, p.date))::int
                FROM public.posteringer p
                WHERE COALESCE(p.posterings_date, p.posting_date, p.date) IS NOT NULL
                ORDER BY 1 DESC;", connection);
            var years = new List<int>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                years.Add(reader.GetInt32(0));
            }

            return years;
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
                       CONCAT_WS(' · ', NULLIF(a.main_account, ''), NULLIF(a.sub_account, ''), NULLIF(a.context, '')),
                       CONCAT_WS(' · ', NULLIF(pg.posting_group, ''), NULLIF(pg.context, '')),
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
            ValidatePostingUpdate(update, requireId: false);
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                UPDATE public.posteringer
                SET account_number = @account_id,
                    posting_group_id = @posting_group_id,
                    user_id = @user_id,
                    text = @text,
                    amount = @amount,
                    posterings_date = @posterings_date,
                    document = CASE WHEN mp_key IS NOT NULL THEN NULL ELSE @document END,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @id;", connection);
            AddText(command, "id", id); AddNullableText(command, "account_id", update.AccountId); AddNullableText(command, "posting_group_id", update.PostingGroupId); AddNullableText(command, "user_id", update.UserId); AddNullableText(command, "text", update.Text); AddNumeric(command, "amount", update.Amount); AddNullableDate(command, "posterings_date", update.PostingDate); AddNullableText(command, "document", update.Document);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }

        public async Task<FinanceAdminPostingDetailDto> CreateAdminPostingAsync(FinanceAdminPostingUpdateDto update, CancellationToken cancellationToken)
        {
            ValidatePostingUpdate(update, requireId: true);
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                INSERT INTO public.posteringer
                    (id, date, posting_date, text, amount, user_id, account_number, posting_group_id, document, posterings_date, created_at, updated_at)
                VALUES
                    (@id, @date, @date, @text, @amount, @user_id, @account_id, @posting_group_id, @document, @posterings_date, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
            AddText(command, "id", update.Id.Trim());
            AddNullableDate(command, "date", string.IsNullOrWhiteSpace(update.Date) ? update.PostingDate : update.Date);
            AddNullableText(command, "text", update.Text);
            AddNumeric(command, "amount", update.Amount);
            AddNullableText(command, "user_id", update.UserId);
            AddNullableText(command, "account_id", update.AccountId);
            AddNullableText(command, "posting_group_id", update.PostingGroupId);
            AddNullableText(command, "document", update.Document);
            AddNullableDate(command, "posterings_date", update.PostingDate);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return await GetAdminPostingDetailAsync(update.Id.Trim(), cancellationToken);
        }

        public async Task<FinanceAdminPostingDetailDto> DuplicateAdminPostingAsync(string id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            var newId = $"{id}-COPY";
            await using var command = new NpgsqlCommand(@"
                INSERT INTO public.posteringer
                    (id, date, posting_date, text, amount, bank_account_key, mp_key, user_id, account_number, posting_group_id, document, posterings_date, created_at, updated_at)
                SELECT @new_id, date, posting_date, text, amount, bank_account_key, mp_key, user_id, account_number, posting_group_id, document, posterings_date, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM public.posteringer
                WHERE id = @id;", connection);
            AddText(command, "id", id);
            AddText(command, "new_id", newId);
            try
            {
                if (await command.ExecuteNonQueryAsync(cancellationToken) != 1) return null;
            }
            catch (PostgresException exception) when (exception.SqlState == "23505")
            {
                throw new ArgumentException($"Posteringen {newId} findes allerede.");
            }
            return await GetAdminPostingDetailAsync(newId, cancellationToken);
        }

        public async Task<bool> DeleteAdminPostingAsync(string id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_writeConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM public.posteringer WHERE id = @id;", connection);
            AddText(command, "id", id);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }

        private static void ValidatePostingUpdate(FinanceAdminPostingUpdateDto update, bool requireId)
        {
            if (update == null) throw new ArgumentException("Posteringen mangler.");
            if (requireId && string.IsNullOrWhiteSpace(update.Id)) throw new ArgumentException("ID må ikke være tomt.");
            if (update.Amount != update.Amount || update.Amount == decimal.MaxValue || update.Amount == decimal.MinValue) throw new ArgumentException("Beløbet er ugyldigt.");
        }

        private static async Task<IReadOnlyList<FinanceSelectOptionDto>> ReadSelectOptionsAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
        {
            var result = new List<FinanceSelectOptionDto>();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) result.Add(new FinanceSelectOptionDto { Id = reader.GetString(0), Label = reader.IsDBNull(1) ? reader.GetString(0) : reader.GetString(1) });
            return result;
        }

        private static async Task<IReadOnlyList<FinancePostingGroupDto>> ReadPostingGroupDefinitionsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            var result = new List<FinancePostingGroupDto>();
            await using var command = new NpgsqlCommand("SELECT id, COALESCE(posting_group, ''), COALESCE(context, '') FROM public.postering_group ORDER BY posting_group, context, id;", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) result.Add(new FinancePostingGroupDto { Id = reader.GetString(0), PostingGroup = reader.GetString(1), Context = reader.GetString(2) });
            return result;
        }

        private static void ValidatePostingGroup(FinancePostingGroupUpdateDto update)
        {
            if (update == null || string.IsNullOrWhiteSpace(update.Id)) throw new ArgumentException("ID må ikke være tomt.");
        }

        private static async Task<IReadOnlyList<FinanceBudgetDto>> ReadBudgetsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            var result = new List<FinanceBudgetDto>();
            await using var command = new NpgsqlCommand(@"
                SELECT id, COALESCE(account_id, ''), COALESCE(postering_group_id, ''), COALESCE(year_actual, 0),
                       COALESCE(forecast, 0), COALESCE(forecast_type, '')
                FROM public.forecast
                ORDER BY year_actual DESC, id;", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) result.Add(MapBudget(reader));
            return result;
        }

        private static async Task<FinanceBudgetDto> ReadBudgetAsync(NpgsqlConnection connection, string id, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                SELECT id, COALESCE(account_id, ''), COALESCE(postering_group_id, ''), COALESCE(year_actual, 0),
                       COALESCE(forecast, 0), COALESCE(forecast_type, '')
                FROM public.forecast WHERE id = @id;", connection);
            AddText(command, "id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? MapBudget(reader) : null;
        }

        private static FinanceBudgetDto MapBudget(NpgsqlDataReader reader) => new()
        {
            Id = reader.GetString(0),
            AccountId = reader.GetString(1),
            PostingGroupId = reader.GetString(2),
            YearActual = reader.GetInt32(3),
            Forecast = reader.GetFieldValue<decimal>(4),
            ForecastType = reader.GetString(5)
        };

        private static void ValidateBudget(FinanceBudgetUpdateDto update)
        {
            if (update == null) throw new ArgumentException("Budgetposten mangler.");
            if (string.IsNullOrWhiteSpace(update.Id)) throw new ArgumentException("ID må ikke være tomt.");
            if (string.IsNullOrWhiteSpace(update.AccountId)) throw new ArgumentException("Account ID må ikke være tomt.");
            if (update.YearActual < 1) throw new ArgumentException("År skal være gyldigt.");
        }

        private static async Task EnsureBudgetReferencesAsync(NpgsqlConnection connection, FinanceBudgetUpdateDto update, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                SELECT
                    EXISTS (SELECT 1 FROM public.account WHERE id = @account_id),
                    (@posting_group_id IS NULL OR EXISTS (SELECT 1 FROM public.postering_group WHERE id = @posting_group_id));", connection);
            AddText(command, "account_id", update.AccountId.Trim());
            AddNullableText(command, "posting_group_id", update.PostingGroupId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            if (!reader.GetBoolean(0)) throw new ArgumentException("Account ID findes ikke i kontoplanen.");
            if (!reader.GetBoolean(1)) throw new ArgumentException("Posteringsgruppe findes ikke.");
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

        private static async Task<IReadOnlyList<FinanceAccountPlanDto>> ReadAccountPlanAsync(
            NpgsqlConnection connection,
            CancellationToken cancellationToken)
        {
            var result = new List<FinanceAccountPlanDto>();
            await using var command = new NpgsqlCommand(@"
                SELECT
                    id,
                    main_account,
                    account_key,
                    sub_account,
                    sub_account_key,
                    context,
                    context_key
                FROM public.account
                ORDER BY account_key NULLS LAST,
                         sub_account_key NULLS LAST,
                         context_key NULLS LAST,
                         id;", connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new FinanceAccountPlanDto
                {
                    Id = reader.GetString(0),
                    MainAccount = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    AccountKey = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                    SubAccount = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    SubAccountKey = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    Context = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    ContextKey = reader.IsDBNull(6) ? null : reader.GetInt64(6)
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
                        COALESCE(b.amount, 0),
                        b.balance,
                        COALESCE((SELECT SUM(p.amount) FROM public.posteringer p WHERE p.bank_account_key = b.id), 0)
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
                        COALESCE(m.amount, 0),
                        NULL::numeric,
                        COALESCE((SELECT SUM(p.amount) FROM public.posteringer p WHERE p.mp_key = m.id), 0)
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
                    Balance = reader.IsDBNull(4) ? null : reader.GetFieldValue<decimal>(4),
                    PostedAmount = reader.GetFieldValue<decimal>(5),
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
        private static void AddNumeric(NpgsqlCommand command, string name, decimal value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;
        private static void AddNullableDate(NpgsqlCommand command, string name, string value)
        {
            var parameter = command.Parameters.Add(name, NpgsqlDbType.Date);
            parameter.Value = DateTime.TryParse(value, out var parsed) ? parsed.Date : DBNull.Value;
        }
        private static void AddNullableDate(NpgsqlCommand command, string name, DateTime? value) => command.Parameters.Add(name, NpgsqlDbType.Date).Value = value?.Date ?? (object)DBNull.Value;
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

    public sealed class FinanceAdminBudgetsDto
    {
        public IReadOnlyList<FinanceBudgetDto> Budgets { get; set; } = Array.Empty<FinanceBudgetDto>();
        public IReadOnlyList<FinanceSelectOptionDto> Accounts { get; set; } = Array.Empty<FinanceSelectOptionDto>();
        public IReadOnlyList<FinanceSelectOptionDto> PostingGroups { get; set; } = Array.Empty<FinanceSelectOptionDto>();
    }

    public sealed class FinanceAccountPlanDto
    {
        public string Id { get; set; } = "";
        public string MainAccount { get; set; } = "";
        public long? AccountKey { get; set; }
        public string SubAccount { get; set; } = "";
        public long? SubAccountKey { get; set; }
        public string Context { get; set; } = "";
        public long? ContextKey { get; set; }
    }

    public sealed class FinanceBudgetDto
    {
        public string Id { get; set; } = "";
        public string AccountId { get; set; } = "";
        public string PostingGroupId { get; set; } = "";
        public int YearActual { get; set; }
        public decimal Forecast { get; set; }
        public string ForecastType { get; set; } = "";
    }

    public sealed class FinanceBudgetUpdateDto
    {
        public string Id { get; set; } = "";
        public string AccountId { get; set; } = "";
        public string PostingGroupId { get; set; } = "";
        public int YearActual { get; set; }
        public decimal Forecast { get; set; }
        public string ForecastType { get; set; } = "";
    }

    public sealed class FinancePostingGroupDto
    {
        public string Id { get; set; } = "";
        public string PostingGroup { get; set; } = "";
        public string Context { get; set; } = "";
    }

    public sealed class FinancePostingGroupUpdateDto
    {
        public string Id { get; set; } = "";
        public string PostingGroup { get; set; } = "";
        public string Context { get; set; } = "";
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
        public decimal? Balance { get; set; }
        public decimal PostedAmount { get; set; }
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
        public string AccountId { get; set; } = "";
        public string Account { get; set; } = "";
        public string PostingGroupId { get; set; } = "";
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
    public sealed class FinanceAdminPostingUpdateDto
    {
        public string Id { get; set; } = "";
        public string Date { get; set; } = "";
        public string AccountId { get; set; } = "";
        public string PostingGroupId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string PostingDate { get; set; } = "";
        public string Text { get; set; } = "";
        public decimal Amount { get; set; }
        public string Document { get; set; } = "";
    }
}
