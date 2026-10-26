using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace GamMaSite.Services;

public static class FinanceMobilePayReferenceTypes
{
    public const string OneTime = "ONE_TIME";
    public const string Recurring = "RECURRING";
}

public interface IFinanceMobilePayReferenceService
{
    Task RecordAsync(
        string merchantReference,
        string paymentType,
        string agreementId,
        string userId,
        string payerName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetPayerNamesAsync(
        IEnumerable<string> merchantReferences,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores the finance-specific, historical connection between a MobilePay
/// provider reference and its payer. The cache is deliberately best-effort:
/// finance availability must never affect payment webhook processing.
/// </summary>
public sealed class FinanceMobilePayReferenceService : IFinanceMobilePayReferenceService
{
    private readonly string _connectionString;

    public FinanceMobilePayReferenceService(IConfiguration configuration)
    {
        _connectionString = BuildConnectionString(configuration);
    }

    public async Task RecordAsync(
        string merchantReference,
        string paymentType,
        string agreementId,
        string userId,
        string payerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) ||
            string.IsNullOrWhiteSpace(merchantReference) ||
            string.IsNullOrWhiteSpace(paymentType) ||
            string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                INSERT INTO public.mobilepay_reference_lookup
                    (merchant_reference, payment_type, agreement_id, user_id, payer_name, created_at, updated_at)
                VALUES
                    (@merchant_reference, @payment_type, @agreement_id, @user_id, @payer_name, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                ON CONFLICT (merchant_reference) DO UPDATE SET
                    payment_type = EXCLUDED.payment_type,
                    agreement_id = EXCLUDED.agreement_id,
                    user_id = EXCLUDED.user_id,
                    payer_name = EXCLUDED.payer_name,
                    updated_at = CURRENT_TIMESTAMP;", connection);
            command.Parameters.Add("merchant_reference", NpgsqlDbType.Text).Value = merchantReference.Trim();
            command.Parameters.Add("payment_type", NpgsqlDbType.Text).Value = paymentType.Trim();
            command.Parameters.Add("agreement_id", NpgsqlDbType.Text).Value = DbText(agreementId);
            command.Parameters.Add("user_id", NpgsqlDbType.Text).Value = userId.Trim();
            command.Parameters.Add("payer_name", NpgsqlDbType.Text).Value = DbText(payerName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // This cache is useful for finance imports, but must not make a
            // successful provider webhook retry or fail.
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> GetPayerNamesAsync(
        IEnumerable<string> merchantReferences,
        CancellationToken cancellationToken = default)
    {
        var references = merchantReferences
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Select(reference => reference.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (references.Length == 0 || string.IsNullOrWhiteSpace(_connectionString))
        {
            return result;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
                SELECT merchant_reference, payer_name
                FROM public.mobilepay_reference_lookup
                WHERE merchant_reference = ANY(@merchant_references)
                  AND NULLIF(BTRIM(payer_name), '') IS NOT NULL;", connection);
            command.Parameters.Add("merchant_references", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = references;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result[reader.GetString(0)] = reader.GetString(1);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // A lookup miss is handled as Unknown by the import flow.
        }

        return result;
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        try
        {
            var configuredConnection = configuration["FinanceWrite:CONNECTION_STRING"] ?? configuration["ConnectionStrings:FinanceWrite"];
            if (!string.IsNullOrWhiteSpace(configuredConnection))
            {
                return new NpgsqlConnectionStringBuilder(configuredConnection)
                {
                    ApplicationName = "GamMaSite Finance MobilePay Reference Cache"
                }.ConnectionString;
            }

            var host = configuration["FinanceWrite:Host"] ?? configuration["Finance:Host"];
            var database = configuration["FinanceWrite:Database"] ?? configuration["Finance:Database"];
            var username = configuration["FinanceWrite:Username"] ?? configuration["Finance:Username"];
            var password = configuration["FinanceWrite:Password"] ?? configuration["Finance:Password"];
            var portValue = configuration["FinanceWrite:Port"] ?? configuration["Finance:Port"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var connection = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(portValue, out var port) ? port : 5432,
                Database = database,
                Username = username,
                Password = password,
                ApplicationName = "GamMaSite Finance MobilePay Reference Cache"
            };
            var sslMode = configuration["FinanceWrite:SslMode"] ?? configuration["Finance:SslMode"];
            var channelBinding = configuration["FinanceWrite:ChannelBinding"] ?? configuration["Finance:ChannelBinding"];
            if (!string.IsNullOrWhiteSpace(sslMode)) connection["SSL Mode"] = sslMode;
            if (!string.IsNullOrWhiteSpace(channelBinding)) connection["Channel Binding"] = channelBinding;
            return connection.ConnectionString;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static object DbText(string value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
