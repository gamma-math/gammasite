using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace GamMaSite.Services
{
    /// <summary>
    /// Imports raw bank and MobilePay exports. Source records are kept separate
    /// from derived ledger entries so categorisation is never overwritten by an import.
    /// </summary>
    public sealed class FinanceImportService
    {
        private readonly string _connectionString;

        public FinanceImportService(IConfiguration configuration)
        {
            var configuredConnection = configuration["FinanceWrite:CONNECTION_STRING"] ?? configuration["ConnectionStrings:FinanceWrite"];
            if (!string.IsNullOrWhiteSpace(configuredConnection))
            {
                try
                {
                    _connectionString = new NpgsqlConnectionStringBuilder(configuredConnection)
                    {
                        ApplicationName = "GamMaSite Finance Write"
                    }.ConnectionString;
                    return;
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException("Finance-skriveforbindelsesstrengen er ugyldig. Brug Npgsql-formatet Host=...;Database=...;Username=...;Password=...;.", exception);
                }
            }

            var host = configuration["FinanceWrite:Host"] ?? configuration["Finance:Host"];
            var database = configuration["FinanceWrite:Database"] ?? configuration["Finance:Database"];
            var username = configuration["FinanceWrite:Username"] ?? configuration["Finance:Username"];
            var password = configuration["FinanceWrite:Password"] ?? configuration["Finance:Password"];
            var portValue = configuration["FinanceWrite:Port"] ?? configuration["Finance:Port"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Finance-databasekonfigurationen mangler. Kontrollér .env.local.");
            }

            var connection = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(portValue, out var port) ? port : 5432,
                Database = database,
                Username = username,
                Password = password,
                ApplicationName = "GamMaSite Finance Write"
            };
            var sslMode = configuration["FinanceWrite:SslMode"] ?? configuration["Finance:SslMode"];
            var channelBinding = configuration["FinanceWrite:ChannelBinding"] ?? configuration["Finance:ChannelBinding"];
            if (!string.IsNullOrWhiteSpace(sslMode)) connection["SSL Mode"] = sslMode;
            if (!string.IsNullOrWhiteSpace(channelBinding)) connection["Channel Binding"] = channelBinding;
            _connectionString = connection.ConnectionString;
        }

        public async Task<FinanceImportResultDto> ImportAsync(
            Stream bankCsv,
            string bankFileName,
            Stream mobilePayCsv,
            string mobilePayFileName,
            bool syncPostings,
            CancellationToken cancellationToken)
        {
            var bankRows = bankCsv == null ? Array.Empty<BankImportRow>() : ReadBankRows(bankCsv);
            var mobilePayRows = mobilePayCsv == null ? Array.Empty<MobilePayImportRow>() : ReadMobilePayRows(mobilePayCsv);

            if (bankRows.Count == 0 && mobilePayRows.Count == 0)
            {
                throw new ArgumentException("Vælg mindst én CSV-fil med mindst én datarække.");
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var result = new FinanceImportResultDto();
            if (bankRows.Count > 0)
            {
                result.Bank = await UpsertBankRowsAsync(connection, transaction, bankRows, cancellationToken);
                await SaveHistoryAsync(connection, transaction, "bank_csv", bankFileName, result.Bank, "Bankkontoudtog importeret.", cancellationToken);
            }

            if (mobilePayRows.Count > 0)
            {
                result.MobilePay = await UpsertMobilePayRowsAsync(connection, transaction, mobilePayRows, cancellationToken);
                await SaveHistoryAsync(connection, transaction, "mobilepay_csv", mobilePayFileName, result.MobilePay, "MobilePay-transaktioner importeret.", cancellationToken);
            }

            if (syncPostings)
            {
                result.Postings = await SyncDerivedPostingsAsync(connection, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            result.ImportedAt = DateTimeOffset.UtcNow.ToString("O");
            return result;
        }

        public async Task<IReadOnlyList<FinanceImportHistoryDto>> GetHistoryAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                SELECT id, import_type, imported_at, COALESCE(file_name, ''), status,
                       rows_processed, rows_inserted, rows_updated, rows_requiring_review,
                       COALESCE(notes, '')
                FROM public.import_history
                ORDER BY imported_at DESC, id DESC;", connection);
            var result = new List<FinanceImportHistoryDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new FinanceImportHistoryDto
                {
                    Id = reader.GetInt64(0),
                    ImportType = reader.GetString(1),
                    ImportedAt = reader.GetFieldValue<DateTimeOffset>(2).ToString("O"),
                    FileName = reader.GetString(3),
                    Status = reader.GetString(4),
                    RowsProcessed = reader.GetInt32(5),
                    RowsInserted = reader.GetInt32(6),
                    RowsUpdated = reader.GetInt32(7),
                    RowsRequiringReview = reader.GetInt32(8),
                    Notes = reader.GetString(9)
                });
            }
            return result;
        }

        /// <summary>
        /// Creates or refreshes automatic postings from the raw bank and MobilePay
        /// tables without requiring another CSV upload.
        /// </summary>
        public async Task<FinancePostingSyncDto> GenerateDerivedPostingsAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var result = await SyncDerivedPostingsAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        private static async Task<FinanceImportBatchDto> UpsertBankRowsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IReadOnlyList<BankImportRow> rows,
            CancellationToken cancellationToken)
        {
            var result = new FinanceImportBatchDto { RowsProcessed = rows.Count };
            foreach (var row in rows)
            {
                await using var command = new NpgsqlCommand(@"
                    INSERT INTO public.bank_account (date, text, amount, balance, is_manual, created_at, updated_at)
                    VALUES (@date, @text, @amount, @balance, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                    ON CONFLICT (date, text, amount, balance) WHERE is_manual = false
                    DO UPDATE SET updated_at = CURRENT_TIMESTAMP
                    RETURNING (xmax = 0);", connection, transaction);
                command.Parameters.Add("date", NpgsqlDbType.Date).Value = row.Date;
                command.Parameters.Add("text", NpgsqlDbType.Text).Value = row.Text;
                command.Parameters.Add("amount", NpgsqlDbType.Numeric).Value = row.Amount;
                command.Parameters.Add("balance", NpgsqlDbType.Numeric).Value = row.Balance;
                var inserted = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
                if (inserted) result.RowsInserted++; else result.RowsUpdated++;
            }
            return result;
        }

        private static async Task<FinanceImportBatchDto> UpsertMobilePayRowsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IReadOnlyList<MobilePayImportRow> rows,
            CancellationToken cancellationToken)
        {
            var result = new FinanceImportBatchDto { RowsProcessed = rows.Count };
            foreach (var row in rows)
            {
                await using var command = new NpgsqlCommand(@"
                    INSERT INTO public.mobilepay
                        (date, timestamp_iso, amount, message, transaction_type, transfer_ref, transfer_date, payment_tx_id, payner_name, is_manual, created_at, updated_at)
                    VALUES
                        (@date, @timestamp_iso, @amount, @message, @transaction_type, @transfer_ref, @transfer_date, @payment_tx_id, @payner_name, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                    ON CONFLICT (timestamp_iso, amount, message, transaction_type, transfer_ref, payment_tx_id) WHERE is_manual = false
                    DO UPDATE SET
                        date = EXCLUDED.date,
                        transfer_date = EXCLUDED.transfer_date,
                        payner_name = EXCLUDED.payner_name,
                        updated_at = CURRENT_TIMESTAMP
                    RETURNING (xmax = 0);", connection, transaction);
                command.Parameters.Add("date", NpgsqlDbType.Date).Value = row.Date;
                command.Parameters.Add("timestamp_iso", NpgsqlDbType.Timestamp).Value = row.Timestamp;
                command.Parameters.Add("amount", NpgsqlDbType.Numeric).Value = row.Amount;
                command.Parameters.Add("message", NpgsqlDbType.Text).Value = DbText(row.Message);
                command.Parameters.Add("transaction_type", NpgsqlDbType.Text).Value = DbText(row.TransactionType);
                command.Parameters.Add("transfer_ref", NpgsqlDbType.Text).Value = DbText(row.TransferReference);
                command.Parameters.Add("transfer_date", NpgsqlDbType.Date).Value = row.TransferDate ?? (object)DBNull.Value;
                command.Parameters.Add("payment_tx_id", NpgsqlDbType.Text).Value = DbText(row.PaymentTransactionId);
                command.Parameters.Add("payner_name", NpgsqlDbType.Text).Value = DbText(row.PayerName);
                var inserted = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
                if (inserted) result.RowsInserted++; else result.RowsUpdated++;
            }
            return result;
        }

        private static async Task<FinancePostingSyncDto> SyncDerivedPostingsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken)
        {
            var result = new FinancePostingSyncDto();

            // A matching MobilePay line supersedes the bank line. Existing
            // postings are deliberately left unchanged and skipped.
            await using (var command = new NpgsqlCommand(@"
                WITH derived AS (
                    SELECT
                        CASE WHEN m.id IS NULL THEN CONCAT('BA-', b.id::text) ELSE CONCAT('MP-', m.id::text) END AS id,
                        CASE WHEN m.id IS NULL THEN b.date ELSE m.date END AS date,
                        CASE WHEN m.id IS NULL THEN b.date ELSE m.date END AS posting_date,
                        CASE
                            WHEN m.id IS NULL THEN CONCAT('Bank: ', COALESCE(b.text, ''))
                            ELSE CONCAT_WS(' · ', CONCAT('MobilePay: ', COALESCE(m.transaction_type, '')), NULLIF(m.message, ''), NULLIF(m.payner_name, ''))
                        END AS text,
                        CASE WHEN m.id IS NULL THEN b.amount ELSE m.amount END AS amount,
                        b.id AS bank_account_key,
                        m.id AS mp_key
                    FROM public.bank_account b
                    LEFT JOIN public.mobilepay m
                      ON NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                    WHERE b.is_manual = false
                )
                INSERT INTO public.posteringer
                    (id, date, posting_date, text, amount, bank_account_key, mp_key, created_at, updated_at)
                SELECT id, date, posting_date, text, amount, bank_account_key, mp_key, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM derived
                ON CONFLICT (id) DO NOTHING
                RETURNING id;", connection, transaction))
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Created++;
                    if (reader.GetString(0).StartsWith("MP-", StringComparison.Ordinal)) result.MobilePayPostings++;
                    else result.BankPostings++;
                }
            }

            // If a later MobilePay import matches an earlier bank import, remove the
            // obsolete automatically derived bank posting. The inverse is handled too.
            await using (var removeBank = new NpgsqlCommand(@"
                DELETE FROM public.posteringer p
                USING public.bank_account b
                WHERE p.id = CONCAT('BA-', b.id::text)
                  AND p.bank_account_key = b.id
                  AND p.mp_key IS NULL
                  AND p.date = b.date
                  AND p.posting_date = b.date
                  AND p.text = CONCAT('Bank: ', COALESCE(b.text, ''))
                  AND p.amount = b.amount
                  AND p.user_id IS NULL
                  AND p.account_number IS NULL
                  AND p.posting_group_id IS NULL
                  AND p.document IS NULL
                  AND EXISTS (
                    SELECT 1 FROM public.mobilepay m
                    WHERE NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                  );", connection, transaction))
            {
                result.Removed += await removeBank.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var removeMobilePay = new NpgsqlCommand(@"
                DELETE FROM public.posteringer p
                USING public.mobilepay m
                WHERE p.id = CONCAT('MP-', m.id::text)
                  AND p.mp_key = m.id
                  AND p.date = m.date
                  AND p.posting_date = m.date
                  AND p.amount = m.amount
                  AND p.text = CONCAT_WS(' · ', CONCAT('MobilePay: ', COALESCE(m.transaction_type, '')), NULLIF(m.message, ''), NULLIF(m.payner_name, ''))
                  AND p.user_id IS NULL
                  AND p.account_number IS NULL
                  AND p.posting_group_id IS NULL
                  AND p.document IS NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM public.bank_account b
                    WHERE NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                  );", connection, transaction))
            {
                result.Removed += await removeMobilePay.ExecuteNonQueryAsync(cancellationToken);
            }

            return result;
        }

        private static async Task SaveHistoryAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            string importType,
            string fileName,
            FinanceImportBatchDto batch,
            string notes,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                INSERT INTO public.import_history
                    (import_type, imported_at, file_name, status, rows_processed, rows_inserted, rows_updated, rows_requiring_review, notes, created_at, updated_at)
                VALUES
                    (@import_type, CURRENT_TIMESTAMP, @file_name, 'completed', @rows_processed, @rows_inserted, @rows_updated, 0, @notes, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection, transaction);
            command.Parameters.Add("import_type", NpgsqlDbType.Text).Value = importType;
            command.Parameters.Add("file_name", NpgsqlDbType.Text).Value = DbText(fileName);
            command.Parameters.Add("rows_processed", NpgsqlDbType.Integer).Value = batch.RowsProcessed;
            command.Parameters.Add("rows_inserted", NpgsqlDbType.Integer).Value = batch.RowsInserted;
            command.Parameters.Add("rows_updated", NpgsqlDbType.Integer).Value = batch.RowsUpdated;
            command.Parameters.Add("notes", NpgsqlDbType.Text).Value = notes;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static IReadOnlyList<BankImportRow> ReadBankRows(Stream stream)
        {
            var records = ReadCsv(stream, "bankkontoudtog");
            var result = new List<BankImportRow>();
            foreach (var record in records)
            {
                var date = ParseDate(Value(record, "dato", "date"), "Dato", record.LineNumber);
                var text = Required(Value(record, "tekst", "text"), "Tekst", record.LineNumber);
                var amount = ParseDecimal(Value(record, "belob", "belab", "amount"), "Beløb", record.LineNumber);
                var balance = ParseDecimal(Value(record, "saldo", "balance"), "Saldo", record.LineNumber);
                result.Add(new BankImportRow(date, text, amount, balance));
            }
            return result;
        }

        private static IReadOnlyList<MobilePayImportRow> ReadMobilePayRows(Stream stream)
        {
            var records = ReadCsv(stream, "MobilePay-transaktioner");
            var result = new List<MobilePayImportRow>();
            foreach (var record in records)
            {
                var date = ParseDate(Value(record, "date", "dato"), "Date", record.LineNumber);
                var timestamp = ParseTimestamp(Value(record, "timestamp", "timestampiso"), "Timestamp", record.LineNumber);
                var amount = ParseDecimal(Value(record, "amount", "belob", "belab"), "Amount", record.LineNumber);
                var message = Required(Value(record, "message", "besked"), "Message", record.LineNumber);
                var transactionType = Required(Value(record, "transactiontype", "transaktionstype"), "Transaction Type", record.LineNumber);
                var transferReference = Required(Value(record, "transferreference", "overforselsreference"), "Transfer Reference", record.LineNumber);
                var transferDate = ParseDate(Required(Value(record, "transferdate", "overforselsdato"), "Transfer Date", record.LineNumber), "Transfer Date", record.LineNumber);
                var paymentTransactionId = Required(Value(record, "paymenttransactionid", "betalingstransaktionsid"), "Payment Transaction ID", record.LineNumber);
                var payerName = Required(Value(record, "username", "payername", "betalersnavn"), "User Name", record.LineNumber);
                result.Add(new MobilePayImportRow(
                    date,
                    timestamp,
                    amount,
                    message,
                    transactionType,
                    transferReference,
                    transferDate,
                    paymentTransactionId,
                    payerName));
            }
            return result;
        }

        private static IReadOnlyList<CsvRecord> ReadCsv(Stream stream, string sourceName)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
            var content = reader.ReadToEnd();
            var separator = DetectSeparator(content);
            var values = ParseCsv(content, separator);
            if (values.Count < 2) throw new ArgumentException($"{sourceName} mangler datarækker.");

            var headers = values[0].Select(NormalizeHeader).ToArray();
            if (headers.Any(string.IsNullOrWhiteSpace) || headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
            {
                throw new ArgumentException($"{sourceName} har tomme eller dublerede kolonneoverskrifter.");
            }

            var records = new List<CsvRecord>();
            for (var index = 1; index < values.Count; index++)
            {
                var row = values[index];
                if (row.All(string.IsNullOrWhiteSpace)) continue;
                if (row.Count != headers.Length)
                {
                    throw new ArgumentException($"{sourceName}: række {index + 1} har {row.Count} felter, men overskriften har {headers.Length}.");
                }
                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var column = 0; column < headers.Length; column++) map[headers[column]] = row[column]?.Trim() ?? "";
                records.Add(new CsvRecord(index + 1, map));
            }
            return records;
        }

        private static char DetectSeparator(string content)
        {
            var header = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)) ?? "";
            return header.Count(character => character == ';') >= header.Count(character => character == ',') ? ';' : ',';
        }

        private static List<List<string>> ParseCsv(string text, char separator)
        {
            var result = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < text.Length && text[index + 1] == '"') { field.Append('"'); index++; }
                    else quoted = !quoted;
                    continue;
                }
                if (!quoted && character == separator)
                {
                    row.Add(field.ToString()); field.Clear(); continue;
                }
                if (!quoted && (character == '\n' || character == '\r'))
                {
                    if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                    row.Add(field.ToString()); field.Clear(); result.Add(row); row = new List<string>(); continue;
                }
                field.Append(character);
            }
            if (quoted) throw new ArgumentException("CSV-filen indeholder et uafsluttet anførselstegn.");
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); result.Add(row); }
            return result;
        }

        private static string NormalizeHeader(string value)
        {
            var decomposed = (value ?? "").Trim().Trim('\uFEFF').Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
                if (character == '\u00e6' || character == '\u00c6')
                {
                    builder.Append("ae");
                    continue;
                }
                if (character == '\u00f8' || character == '\u00d8')
                {
                    builder.Append('o');
                    continue;
                }
                if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            }
            return builder.ToString();
        }

        private static string Value(CsvRecord record, params string[] names)
        {
            foreach (var name in names)
            {
                if (record.Values.TryGetValue(name, out var value)) return value;
            }
            return "";
        }

        private static string Required(string value, string column, int lineNumber)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"CSV-række {lineNumber}: {column} må ikke være tom.");
            return value.Trim();
        }

        private static DateTime ParseDate(string value, string column, int lineNumber)
        {
            if (DateTime.TryParseExact(value?.Trim(), new[] { "dd.MM.yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "d.M.yyyy", "d-M-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
                || DateTime.TryParse(value, CultureInfo.GetCultureInfo("da-DK"), DateTimeStyles.AllowWhiteSpaces, out date)) return date.Date;
            throw new ArgumentException($"CSV-række {lineNumber}: {column} skal være en gyldig dato.");
        }

        private static DateTime? ParseOptionalDate(string value, string column, int lineNumber) => string.IsNullOrWhiteSpace(value) ? null : ParseDate(value, column, lineNumber);

        private static DateTime ParseTimestamp(string value, string column, int lineNumber)
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var offset)) return offset.DateTime;
            if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("da-DK"), DateTimeStyles.AllowWhiteSpaces, out var timestamp)) return timestamp;
            throw new ArgumentException($"CSV-række {lineNumber}: {column} skal være et gyldigt tidspunkt.");
        }

        private static decimal ParseDecimal(string value, string column, int lineNumber)
        {
            var normalized = (value ?? "").Trim().Replace(" ", "").Replace("\u00A0", "");
            if (normalized.Contains(',')) normalized = normalized.Replace(".", "").Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount)) return amount;
            throw new ArgumentException($"CSV-række {lineNumber}: {column} skal være et gyldigt beløb.");
        }

        private static object DbText(string value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private sealed record CsvRecord(int LineNumber, Dictionary<string, string> Values);
        private sealed record BankImportRow(DateTime Date, string Text, decimal Amount, decimal Balance);
        private sealed record MobilePayImportRow(DateTime Date, DateTime Timestamp, decimal Amount, string Message, string TransactionType, string TransferReference, DateTime? TransferDate, string PaymentTransactionId, string PayerName);
    }

    public sealed class FinanceImportResultDto
    {
        public string ImportedAt { get; set; } = "";
        public FinanceImportBatchDto Bank { get; set; } = new();
        public FinanceImportBatchDto MobilePay { get; set; } = new();
        public FinancePostingSyncDto Postings { get; set; } = new();
    }

    public sealed class FinanceImportBatchDto
    {
        public int RowsProcessed { get; set; }
        public int RowsInserted { get; set; }
        public int RowsUpdated { get; set; }
    }

    public sealed class FinancePostingSyncDto
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Removed { get; set; }
        public int BankPostings { get; set; }
        public int MobilePayPostings { get; set; }
    }

    public sealed class FinanceImportHistoryDto
    {
        public long Id { get; set; }
        public string ImportType { get; set; } = "";
        public string ImportedAt { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Status { get; set; } = "";
        public int RowsProcessed { get; set; }
        public int RowsInserted { get; set; }
        public int RowsUpdated { get; set; }
        public int RowsRequiringReview { get; set; }
        public string Notes { get; set; } = "";
    }
}
