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
    /// Imports raw bank, MobilePay and Stripe source data. Source records are kept separate
    /// from derived ledger entries so categorisation is never overwritten by an import.
    /// </summary>
    public sealed class FinanceImportService
    {
        private readonly string _connectionString;
        private readonly IFinanceMobilePayReferenceService _mobilePayReferenceService;
        private static readonly Encoding Windows1252 = CreateWindows1252Encoding();
        private const string UnknownPayerName = "Unknown";

        private static Encoding CreateWindows1252Encoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }

        private static readonly IReadOnlyList<CsvColumnRule> BankCsvSchema = new[]
        {
            new CsvColumnRule("Dato", CsvValueType.Date, true, true, new[] { "dato", "date" }),
            new CsvColumnRule("Tekst", CsvValueType.Text, true, true, new[] { "tekst", "text" }),
            new CsvColumnRule("Beløb", CsvValueType.Decimal, true, true, new[] { "belob", "belab", "amount" }),
            new CsvColumnRule("Saldo", CsvValueType.Decimal, true, true, new[] { "saldo", "balance" }),
        };

        private static readonly IReadOnlyList<CsvColumnRule> MobilePayCsvSchema = new[]
        {
            new CsvColumnRule("Date", CsvValueType.Date, true, true, new[] { "date", "dato" }),
            new CsvColumnRule("Timestamp", CsvValueType.Timestamp, true, true, new[] { "timestamp", "timestampiso" }),
            new CsvColumnRule("Amount", CsvValueType.Decimal, true, true, new[] { "amount", "belob", "belab" }),
            // Message is required as a column, but its value may be empty.
            new CsvColumnRule("Message", CsvValueType.Text, true, false, new[] { "message", "besked" }),
            new CsvColumnRule("Transaction Type", CsvValueType.Text, true, true, new[] { "transactiontype", "transaktionstype" }),
            new CsvColumnRule("Transfer Reference", CsvValueType.Text, true, true, new[] { "transferreference", "overforselsreference" }),
            new CsvColumnRule("Transfer Date", CsvValueType.Date, true, true, new[] { "transferdate", "overforselsdato" }),
            new CsvColumnRule("Payment Transaction ID", CsvValueType.Text, true, true, new[] { "paymenttransactionid", "betalingstransaktionsid" }),
            // Newer API exports intentionally omit the payer's name. It is
            // resolved from the provider reference before we create postings.
            new CsvColumnRule("User Name", CsvValueType.Text, true, false, new[] { "username", "payername", "betalersnavn" }),
            new CsvColumnRule("Merchant Reference", CsvValueType.Text, false, false, new[] { "merchantreference" }),
            new CsvColumnRule("Product", CsvValueType.Text, false, false, new[] { "product" }),
        };

        public FinanceImportService(
            IConfiguration configuration,
            IFinanceMobilePayReferenceService mobilePayReferenceService = null)
        {
            _mobilePayReferenceService = mobilePayReferenceService;
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

            // Enrichment happens before the raw source rows are upserted. The
            // subsequent posting sync only reads public.mobilepay and never
            // calls either the application database or MobilePay.
            if (mobilePayRows.Count > 0)
            {
                mobilePayRows = await EnrichMobilePayRowsAsync(mobilePayRows, cancellationToken);
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

        /// <summary>
        /// Validates the selected CSV files without opening the finance database
        /// or changing any data. This is used by the upload form before import.
        /// </summary>
        public void ValidateCsv(Stream bankCsv, Stream mobilePayCsv)
        {
            var bankRows = bankCsv == null ? Array.Empty<BankImportRow>() : ReadBankRows(bankCsv);
            var mobilePayRows = mobilePayCsv == null ? Array.Empty<MobilePayImportRow>() : ReadMobilePayRows(mobilePayCsv);

            if (bankRows.Count == 0 && mobilePayRows.Count == 0)
            {
                throw new ArgumentException("Vælg mindst én CSV-fil med mindst én datarække.");
            }
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
        /// Creates or refreshes automatic postings from the raw bank, MobilePay and Stripe
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

        public async Task<FinanceImportBatchDto> ImportStripeTransactionsAsync(
            IReadOnlyList<StripeBalanceTransaction> rows,
            CancellationToken cancellationToken)
        {
            if (rows == null || rows.Count == 0)
            {
                return new FinanceImportBatchDto();
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var result = await UpsertStripeRowsAsync(connection, transaction, rows, cancellationToken);
            await SaveHistoryAsync(
                connection,
                transaction,
                "stripe_webhook",
                rows[0].PayoutId,
                result,
                "Stripe balance transactions imported from payout webhook.",
                cancellationToken);

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
                        (date, timestamp_iso, amount, message, transaction_type, transfer_ref, transfer_date, payment_tx_id, payner_name, merchant_reference, source_product, is_manual, created_at, updated_at)
                    VALUES
                        (@date, @timestamp_iso, @amount, @message, @transaction_type, @transfer_ref, @transfer_date, @payment_tx_id, @payner_name, @merchant_reference, @source_product, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                    ON CONFLICT (timestamp_iso, amount, message, transaction_type, transfer_ref, payment_tx_id) WHERE is_manual = false
                    DO UPDATE SET
                        date = EXCLUDED.date,
                        transfer_date = EXCLUDED.transfer_date,
                        payner_name = EXCLUDED.payner_name,
                        merchant_reference = EXCLUDED.merchant_reference,
                        source_product = EXCLUDED.source_product,
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
                command.Parameters.Add("merchant_reference", NpgsqlDbType.Text).Value = DbText(row.MerchantReference);
                command.Parameters.Add("source_product", NpgsqlDbType.Text).Value = DbText(row.SourceProduct);
                var inserted = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
                if (inserted) result.RowsInserted++; else result.RowsUpdated++;
            }
            return result;
        }

        private static async Task<FinanceImportBatchDto> UpsertStripeRowsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IReadOnlyList<StripeBalanceTransaction> rows,
            CancellationToken cancellationToken)
        {
            var result = new FinanceImportBatchDto { RowsProcessed = rows.Count };
            foreach (var row in rows)
            {
                await using var command = new NpgsqlCommand(@"
                    INSERT INTO public.stripe
                        (balance_transaction_id, payout_id, date, available_date, payout_date,
                         amount, fee, net, currency, transaction_type, reporting_category,
                         source_id, payment_intent_id, description, user_id, payer_name, receipt_url,
                         is_manual, created_at, updated_at)
                    VALUES
                        (@balance_transaction_id, @payout_id, @date, @available_date, @payout_date,
                         @amount, @fee, @net, @currency, @transaction_type, @reporting_category,
                         @source_id, @payment_intent_id, @description, @user_id, @payer_name, @receipt_url,
                         false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                    ON CONFLICT (balance_transaction_id) WHERE is_manual = false
                    DO UPDATE SET
                        payout_id = EXCLUDED.payout_id,
                        date = EXCLUDED.date,
                        available_date = EXCLUDED.available_date,
                        payout_date = EXCLUDED.payout_date,
                        amount = EXCLUDED.amount,
                        fee = EXCLUDED.fee,
                        net = EXCLUDED.net,
                        currency = EXCLUDED.currency,
                        transaction_type = EXCLUDED.transaction_type,
                        reporting_category = EXCLUDED.reporting_category,
                        source_id = EXCLUDED.source_id,
                        payment_intent_id = EXCLUDED.payment_intent_id,
                        description = EXCLUDED.description,
                        user_id = EXCLUDED.user_id,
                        payer_name = EXCLUDED.payer_name,
                        receipt_url = EXCLUDED.receipt_url,
                        updated_at = CURRENT_TIMESTAMP
                    RETURNING (xmax = 0);", connection, transaction);
                command.Parameters.Add("balance_transaction_id", NpgsqlDbType.Text).Value = row.BalanceTransactionId;
                command.Parameters.Add("payout_id", NpgsqlDbType.Text).Value = DbText(row.PayoutId);
                command.Parameters.Add("date", NpgsqlDbType.Date).Value = row.Date.Date;
                command.Parameters.Add("available_date", NpgsqlDbType.Date).Value = row.AvailableDate.Date;
                command.Parameters.Add("payout_date", NpgsqlDbType.Date).Value = row.PayoutDate?.Date ?? (object)DBNull.Value;
                command.Parameters.Add("amount", NpgsqlDbType.Numeric).Value = row.Amount;
                command.Parameters.Add("fee", NpgsqlDbType.Numeric).Value = row.Fee;
                command.Parameters.Add("net", NpgsqlDbType.Numeric).Value = row.Net;
                command.Parameters.Add("currency", NpgsqlDbType.Text).Value = DbText(row.Currency);
                command.Parameters.Add("transaction_type", NpgsqlDbType.Text).Value = DbText(row.TransactionType);
                command.Parameters.Add("reporting_category", NpgsqlDbType.Text).Value = DbText(row.ReportingCategory);
                command.Parameters.Add("source_id", NpgsqlDbType.Text).Value = DbText(row.SourceId);
                command.Parameters.Add("payment_intent_id", NpgsqlDbType.Text).Value = DbText(row.PaymentIntentId);
                command.Parameters.Add("description", NpgsqlDbType.Text).Value = DbText(row.Description);
                command.Parameters.Add("user_id", NpgsqlDbType.Text).Value = DbText(row.UserId);
                command.Parameters.Add("payer_name", NpgsqlDbType.Text).Value = DbText(row.PayerName);
                command.Parameters.Add("receipt_url", NpgsqlDbType.Text).Value = DbText(row.ReceiptUrl);
                var inserted = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
                if (inserted) result.RowsInserted++; else result.RowsUpdated++;
            }
            return result;
        }

        private async Task<IReadOnlyList<MobilePayImportRow>> EnrichMobilePayRowsAsync(
            IReadOnlyList<MobilePayImportRow> rows,
            CancellationToken cancellationToken)
        {
            var missingNames = rows
                .Where(row => string.IsNullOrWhiteSpace(row.PayerName))
                .ToArray();

            if (missingNames.Length == 0)
            {
                return rows;
            }

            var payerNames = _mobilePayReferenceService == null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : await _mobilePayReferenceService.GetPayerNamesAsync(
                    missingNames.Select(row => row.MerchantReference),
                    cancellationToken);

            return rows.Select(row =>
            {
                if (!string.IsNullOrWhiteSpace(row.PayerName))
                {
                    return row;
                }

                payerNames.TryGetValue(row.MerchantReference ?? "", out var payerName);
                return row with { PayerName = string.IsNullOrWhiteSpace(payerName) ? UnknownPayerName : payerName };
            }).ToArray();
        }

        private static async Task<FinancePostingSyncDto> SyncDerivedPostingsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken)
        {
            var result = new FinancePostingSyncDto();

            await SyncStripeDerivedPostingsAsync(connection, transaction, result, cancellationToken);

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
                            ELSE CONCAT_WS(
                                ' · ',
                                CONCAT('MobilePay: ', COALESCE(m.transaction_type, '')),
                                CASE
                                    WHEN NULLIF(TRIM(m.message), '') IS NULL THEN NULLIF(TRIM(m.payner_name), '')
                                    WHEN NULLIF(TRIM(m.payner_name), '') IS NULL THEN NULLIF(TRIM(m.message), '')
                                    WHEN LEFT(TRIM(m.message), LENGTH(TRIM(m.payner_name)) + 3) = TRIM(m.payner_name) || ' · '
                                        THEN TRIM(m.message)
                                    ELSE CONCAT_WS(' · ', NULLIF(TRIM(m.payner_name), ''), NULLIF(TRIM(m.message), ''))
                                END
                            )
                        END AS text,
                        CASE WHEN m.id IS NULL THEN b.amount ELSE m.amount END AS amount,
                        b.id AS bank_account_key,
                        m.id AS mp_key,
                        CASE WHEN m.id IS NULL THEN NULL ELSE NULLIF(TRIM(lookup.user_id), '') END AS user_id
                    FROM public.bank_account b
                    LEFT JOIN public.mobilepay m
                      ON NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                    LEFT JOIN public.mobilepay_reference_lookup lookup
                      ON lookup.merchant_reference = NULLIF(TRIM(m.merchant_reference), '')
                    WHERE b.is_manual = false
                      AND (m.id IS NOT NULL OR NOT EXISTS (
                          SELECT 1
                          FROM public.stripe s
                          WHERE s.is_manual = false
                            AND s.payout_date = b.date
                            AND NULLIF(TRIM(s.payout_id), '') IS NOT NULL
                            AND b.text ILIKE '%STRIPE%'
                          GROUP BY s.payout_id, s.payout_date, s.currency
                          HAVING SUM(s.net) = b.amount
                             AND (SELECT COUNT(*)
                                  FROM public.bank_account b2
                                  WHERE b2.is_manual = false
                                    AND b2.date = b.date
                                    AND b2.amount = b.amount
                                    AND b2.text ILIKE '%STRIPE%') = 1
                      ))
                )
                INSERT INTO public.posteringer
                    (id, date, posting_date, text, amount, bank_account_key, mp_key, user_id, created_at, updated_at)
                SELECT id, date, posting_date, text, amount, bank_account_key, mp_key, user_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
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

            // A MobilePay reference may have been enriched after the original
            // derived posting was created. Fill the user ID without overwriting
            // an existing manual assignment.
            await using (var updateMobilePayUsers = new NpgsqlCommand(@"
                UPDATE public.posteringer p
                SET user_id = lookup.user_id,
                    updated_at = CURRENT_TIMESTAMP
                FROM public.mobilepay m
                JOIN public.mobilepay_reference_lookup lookup
                  ON lookup.merchant_reference = NULLIF(TRIM(m.merchant_reference), '')
                WHERE p.id = CONCAT('MP-', m.id::text)
                  AND p.mp_key = m.id
                  AND p.user_id IS NULL
                  AND NULLIF(TRIM(lookup.user_id), '') IS NOT NULL;", connection, transaction))
            {
                await updateMobilePayUsers.ExecuteNonQueryAsync(cancellationToken);
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
                  AND (
                      p.text = CONCAT_WS(
                          ' · ',
                          CONCAT('MobilePay: ', COALESCE(m.transaction_type, '')),
                          CASE
                              WHEN NULLIF(TRIM(m.message), '') IS NULL THEN NULLIF(TRIM(m.payner_name), '')
                              WHEN NULLIF(TRIM(m.payner_name), '') IS NULL THEN NULLIF(TRIM(m.message), '')
                              WHEN LEFT(TRIM(m.message), LENGTH(TRIM(m.payner_name)) + 3) = TRIM(m.payner_name) || ' · '
                                  THEN TRIM(m.message)
                              ELSE CONCAT_WS(' · ', NULLIF(TRIM(m.payner_name), ''), NULLIF(TRIM(m.message), ''))
                          END
                      )
                      OR p.text = CONCAT_WS(' · ', CONCAT('MobilePay: ', COALESCE(m.transaction_type, '')), NULLIF(m.message, ''), NULLIF(m.payner_name, ''))
                  )
                  AND p.account_number IS NULL
                  AND p.posting_group_id IS NULL
                  AND p.document IS NULL
                   AND p.user_id IS NULL
                   AND NOT EXISTS (
                    SELECT 1 FROM public.bank_account b
                    WHERE NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                  );", connection, transaction))
            {
                result.Removed += await removeMobilePay.ExecuteNonQueryAsync(cancellationToken);
            }

            await RemoveObsoleteStripePostingsAsync(connection, transaction, result, cancellationToken);
            await RemoveObsoleteStripeBankPostingsAsync(connection, transaction, result, cancellationToken);

            return result;
        }

        private static async Task SyncStripeDerivedPostingsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            FinancePostingSyncDto result,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                WITH stripe_payouts AS (
                    SELECT payout_id, payout_date, currency, SUM(net) AS net
                    FROM public.stripe
                    WHERE is_manual = false
                      AND NULLIF(TRIM(payout_id), '') IS NOT NULL
                      AND payout_date IS NOT NULL
                    GROUP BY payout_id, payout_date, currency
                ),
                stripe_candidates AS (
                    SELECT
                        sp.payout_id,
                        b.id AS bank_account_key,
                        COUNT(*) OVER (PARTITION BY sp.payout_id, sp.payout_date, sp.currency) AS payout_match_count,
                        COUNT(*) OVER (PARTITION BY b.id) AS bank_match_count
                    FROM stripe_payouts sp
                    JOIN public.bank_account b
                      ON b.is_manual = false
                     AND b.date = sp.payout_date
                     AND b.amount = sp.net
                     AND b.text ILIKE '%STRIPE%'
                ),
                stripe_matches AS (
                    SELECT payout_id, bank_account_key
                    FROM stripe_candidates
                    WHERE payout_match_count = 1
                      AND bank_match_count = 1
                ),
                derived AS (
                    SELECT
                        CONCAT('ST-', s.id::text) AS id,
                        s.date,
                        s.date AS posting_date,
                        CONCAT_WS(
                            ' - ',
                            'Stripe: Amount',
                            COALESCE(NULLIF(TRIM(s.payer_name), ''), NULLIF(TRIM(s.user_id), ''), 'Unknown'),
                            NULLIF(TRIM(s.description), '')
                        ) AS text,
                        s.amount,
                        sm.bank_account_key,
                        s.user_id,
                        s.id AS stripe_key,
                        s.receipt_url AS document
                    FROM public.stripe s
                    JOIN stripe_matches sm ON sm.payout_id = s.payout_id
                    WHERE s.is_manual = false
                      AND s.amount <> 0
                      AND NOT EXISTS (
                          SELECT 1
                          FROM public.mobilepay m
                          JOIN public.bank_account b
                            ON NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                          WHERE b.id = sm.bank_account_key
                      )

                    UNION ALL

                    SELECT
                        CONCAT('ST-', s.id::text, '-FEE') AS id,
                        s.date,
                        s.date AS posting_date,
                        CONCAT_WS(
                            ' - ',
                            'Stripe: Fee',
                            COALESCE(NULLIF(TRIM(s.payer_name), ''), NULLIF(TRIM(s.user_id), ''), 'Unknown'),
                            NULLIF(TRIM(s.description), '')
                        ) AS text,
                        -s.fee AS amount,
                        sm.bank_account_key,
                        s.user_id,
                        s.id AS stripe_key,
                        s.receipt_url AS document
                    FROM public.stripe s
                    JOIN stripe_matches sm ON sm.payout_id = s.payout_id
                    WHERE s.is_manual = false
                      AND s.fee <> 0
                      AND NOT EXISTS (
                          SELECT 1
                          FROM public.mobilepay m
                          JOIN public.bank_account b
                            ON NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                          WHERE b.id = sm.bank_account_key
                      )
                )
                INSERT INTO public.posteringer
                    (id, date, posting_date, text, amount, bank_account_key, user_id, stripe_key, document, created_at, updated_at)
                SELECT id, date, posting_date, text, amount, bank_account_key, user_id, stripe_key, document, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM derived
                ON CONFLICT (id) DO UPDATE
                    SET document = EXCLUDED.document
                    WHERE public.posteringer.document IS NULL
                      AND EXCLUDED.document IS NOT NULL
                RETURNING id;", connection, transaction);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Created++;
                result.StripePostings++;
            }
        }

        private static async Task RemoveObsoleteStripePostingsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            FinancePostingSyncDto result,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                WITH stripe_payouts AS (
                    SELECT payout_id, payout_date, currency, SUM(net) AS net
                    FROM public.stripe
                    WHERE is_manual = false
                      AND NULLIF(TRIM(payout_id), '') IS NOT NULL
                      AND payout_date IS NOT NULL
                    GROUP BY payout_id, payout_date, currency
                ), stripe_candidates AS (
                    SELECT sp.payout_id, b.id AS bank_account_key,
                           COUNT(*) OVER (PARTITION BY sp.payout_id, sp.payout_date, sp.currency) AS payout_match_count,
                           COUNT(*) OVER (PARTITION BY b.id) AS bank_match_count
                    FROM stripe_payouts sp
                    JOIN public.bank_account b
                      ON b.is_manual = false AND b.date = sp.payout_date
                     AND b.amount = sp.net AND b.text ILIKE '%STRIPE%'
                ), stripe_matches AS (
                    SELECT payout_id, bank_account_key
                    FROM stripe_candidates
                    WHERE payout_match_count = 1 AND bank_match_count = 1
                )
                DELETE FROM public.posteringer p
                WHERE p.id LIKE 'ST-%'
                  AND p.stripe_key IS NOT NULL
                  AND p.account_number IS NULL
                  AND p.posting_group_id IS NULL
                  AND (
                      p.document IS NULL
                      OR EXISTS (
                          SELECT 1
                          FROM public.stripe s
                          WHERE s.id = p.stripe_key
                            AND NULLIF(TRIM(p.document), '') = NULLIF(TRIM(s.receipt_url), '')
                      )
                  )
                  AND (
                      NOT EXISTS (
                          SELECT 1
                          FROM public.stripe s
                          JOIN stripe_matches sm ON sm.payout_id = s.payout_id
                          WHERE s.id = p.stripe_key
                            AND s.is_manual = false
                      )
                      OR EXISTS (
                          SELECT 1
                          FROM public.bank_account b
                          JOIN public.mobilepay m
                            ON NULLIF(TRIM(m.transfer_ref), '') = NULLIF(TRIM(b.text), '')
                          WHERE b.id = p.bank_account_key
                      )
                  )
                  ;", connection, transaction);
            result.Removed += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task RemoveObsoleteStripeBankPostingsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            FinancePostingSyncDto result,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(@"
                WITH stripe_payouts AS (
                    SELECT payout_id, payout_date, currency, SUM(net) AS net
                    FROM public.stripe
                    WHERE is_manual = false
                      AND NULLIF(TRIM(payout_id), '') IS NOT NULL
                      AND payout_date IS NOT NULL
                    GROUP BY payout_id, payout_date, currency
                ), stripe_candidates AS (
                    SELECT sp.payout_id, b.id AS bank_account_key,
                           COUNT(*) OVER (PARTITION BY sp.payout_id, sp.payout_date, sp.currency) AS payout_match_count,
                           COUNT(*) OVER (PARTITION BY b.id) AS bank_match_count
                    FROM stripe_payouts sp
                    JOIN public.bank_account b
                      ON b.is_manual = false AND b.date = sp.payout_date
                     AND b.amount = sp.net AND b.text ILIKE '%STRIPE%'
                ), stripe_matches AS (
                    SELECT payout_id, bank_account_key
                    FROM stripe_candidates
                    WHERE payout_match_count = 1 AND bank_match_count = 1
                )
                DELETE FROM public.posteringer p
                USING public.bank_account b
                WHERE p.id = CONCAT('BA-', b.id::text)
                  AND p.bank_account_key = b.id
                  AND p.mp_key IS NULL
                  AND p.stripe_key IS NULL
                  AND p.date = b.date
                  AND p.posting_date = b.date
                  AND p.text = CONCAT('Bank: ', COALESCE(b.text, ''))
                  AND p.amount = b.amount
                  AND p.user_id IS NULL
                  AND p.account_number IS NULL
                  AND p.posting_group_id IS NULL
                  AND p.document IS NULL
                  AND EXISTS (SELECT 1 FROM stripe_matches sm WHERE sm.bank_account_key = b.id);", connection, transaction);
            result.Removed += await command.ExecuteNonQueryAsync(cancellationToken);
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
            var records = ReadCsv(stream, "bankkontoudtog", BankCsvSchema);
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
            var records = ReadCsv(stream, "MobilePay-transaktioner", MobilePayCsvSchema);
            var result = new List<MobilePayImportRow>();
            foreach (var record in records)
            {
                var date = ParseDate(Value(record, "date", "dato"), "Date", record.LineNumber);
                var timestamp = ParseTimestamp(Value(record, "timestamp", "timestampiso"), "Timestamp", record.LineNumber);
                var amount = ParseDecimal(Value(record, "amount", "belob", "belab"), "Amount", record.LineNumber);
                // MobilePay can export an empty message. The payer name is used
                // as the visible fallback when source/posting text is composed.
                var message = Value(record, "message", "besked").Trim();
                var transactionType = Required(Value(record, "transactiontype", "transaktionstype"), "Transaction Type", record.LineNumber);
                var transferReference = Required(Value(record, "transferreference", "overforselsreference"), "Transfer Reference", record.LineNumber);
                var transferDate = ParseDate(Required(Value(record, "transferdate", "overforselsdato"), "Transfer Date", record.LineNumber), "Transfer Date", record.LineNumber);
                var paymentTransactionId = Required(Value(record, "paymenttransactionid", "betalingstransaktionsid"), "Payment Transaction ID", record.LineNumber);
                var payerName = Value(record, "username", "payername", "betalersnavn").Trim();
                var merchantReference = Value(record, "merchantreference").Trim();
                var sourceProduct = Value(record, "product").Trim();
                result.Add(new MobilePayImportRow(
                    date,
                    timestamp,
                    amount,
                    message,
                    transactionType,
                    transferReference,
                    transferDate,
                    paymentTransactionId,
                    payerName,
                    merchantReference,
                    sourceProduct));
            }
            return result;
        }

        private static IReadOnlyList<CsvRecord> ReadCsv(Stream stream, string sourceName, IReadOnlyList<CsvColumnRule> schema)
        {
            var content = ReadCsvText(stream);
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
            ValidateCsvSchema(sourceName, headers, records, schema);
            return records;
        }

        private static string ReadCsvText(Stream stream)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();

            try
            {
                // Use strict UTF-8 first so legacy ANSI/Windows-1252 files can
                // be detected instead of silently becoming replacement chars.
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                    .GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                // Danish bank exports are commonly Windows-1252. Decode with
                // the actual code page so punctuation such as smart quotes
                // and the euro sign is preserved as well as Danish letters.
                return Windows1252.GetString(bytes);
            }
        }

        private static void ValidateCsvSchema(
            string sourceName,
            IReadOnlyList<string> headers,
            IReadOnlyList<CsvRecord> records,
            IReadOnlyList<CsvColumnRule> schema)
        {
            foreach (var column in schema)
            {
                var header = column.Names.FirstOrDefault(name => headers.Contains(name, StringComparer.Ordinal));
                if (header == null)
                {
                    if (column.RequiredColumn)
                    {
                        throw new ArgumentException($"{sourceName} mangler kolonnen {column.Label}.");
                    }

                    continue;
                }

                foreach (var record in records)
                {
                    var value = record.Values[header];
                    if (column.RequiredValue && string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException($"CSV-række {record.LineNumber}: {column.Label} må ikke være tom.");
                    }

                    if (string.IsNullOrWhiteSpace(value)) continue;
                    ValidateCsvValue(value, column, record.LineNumber);
                }
            }
        }

        private static void ValidateCsvValue(string value, CsvColumnRule column, int lineNumber)
        {
            switch (column.Type)
            {
                case CsvValueType.Date:
                    ParseDate(value, column.Label, lineNumber);
                    break;
                case CsvValueType.Timestamp:
                    ParseTimestamp(value, column.Label, lineNumber);
                    break;
                case CsvValueType.Decimal:
                    ParseDecimal(value, column.Label, lineNumber);
                    break;
                case CsvValueType.Text:
                    break;
            }
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

        private enum CsvValueType
        {
            Text,
            Date,
            Timestamp,
            Decimal,
        }

        private sealed record CsvColumnRule(
            string Label,
            CsvValueType Type,
            bool RequiredColumn,
            bool RequiredValue,
            string[] Names);

        private sealed record CsvRecord(int LineNumber, Dictionary<string, string> Values);
        private sealed record BankImportRow(DateTime Date, string Text, decimal Amount, decimal Balance);
        private sealed record MobilePayImportRow(
            DateTime Date,
            DateTime Timestamp,
            decimal Amount,
            string Message,
            string TransactionType,
            string TransferReference,
            DateTime? TransferDate,
            string PaymentTransactionId,
            string PayerName,
            string MerchantReference,
            string SourceProduct);
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
        public int StripePostings { get; set; }
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
