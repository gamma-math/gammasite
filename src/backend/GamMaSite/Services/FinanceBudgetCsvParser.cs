using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GamMaSite.Services
{
    internal static class FinanceBudgetCsvParser
    {
        private static readonly string[] RequiredColumns =
        {
            "id",
            "account number",
            "account",
            "postinggroup id",
            "postinggroup",
            "year",
            "amount",
            "budgettype"
        };

        public static IReadOnlyList<FinanceBudgetCsvRow> Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("CSV-filen er tom.");
            }

            var records = ReadRecords(content);
            if (records.Count == 0)
            {
                throw new ArgumentException("CSV-filen er tom.");
            }

            var header = records[0];
            var indexes = RequiredColumns.ToDictionary(
                column => column,
                column => FindColumn(header, column),
                StringComparer.OrdinalIgnoreCase);

            foreach (var column in RequiredColumns)
            {
                if (indexes[column] < 0)
                {
                    throw new ArgumentException($"CSV-filen mangler kolonnen '{column}'.");
                }
            }

            var rows = new List<FinanceBudgetCsvRow>();
            for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
            {
                var values = records[recordIndex];
                if (values.All(string.IsNullOrWhiteSpace)) continue;

                var rowNumber = recordIndex + 1;
                if (values.Count != header.Count)
                {
                    throw new ArgumentException($"CSV-række {rowNumber} har et forkert antal kolonner.");
                }

                var id = Value(values, indexes["id"]);
                var accountId = Value(values, indexes["account number"]);
                var postingGroupId = NullableValue(values, indexes["postinggroup id"]);
                var yearValue = Value(values, indexes["year"]);
                var amountValue = Value(values, indexes["amount"]);
                var forecastType = NullableValue(values, indexes["budgettype"]);

                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException($"CSV-række {rowNumber}: ID må ikke være tomt.");
                }

                if (string.IsNullOrWhiteSpace(accountId))
                {
                    throw new ArgumentException($"CSV-række {rowNumber}: Account number må ikke være tomt.");
                }

                if (!int.TryParse(yearValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
                {
                    throw new ArgumentException($"CSV-række {rowNumber}: År skal være et helt tal.");
                }

                if (!TryParseAmount(amountValue, out var amount))
                {
                    throw new ArgumentException($"CSV-række {rowNumber}: Amount skal være et gyldigt tal.");
                }

                rows.Add(new FinanceBudgetCsvRow
                {
                    RowNumber = rowNumber,
                    Update = new FinanceBudgetUpdateDto
                    {
                        Id = id,
                        AccountId = accountId,
                        PostingGroupId = postingGroupId,
                        YearActual = year,
                        Forecast = amount,
                        ForecastType = forecastType ?? ""
                    }
                });
            }

            if (rows.Count == 0)
            {
                throw new ArgumentException("CSV-filen indeholder ingen budgetrækker.");
            }

            return rows;
        }

        private static int FindColumn(IReadOnlyList<string> header, string expected)
        {
            var normalizedExpected = NormalizeHeader(expected);
            for (var index = 0; index < header.Count; index++)
            {
                if (NormalizeHeader(header[index]) == normalizedExpected) return index;
            }

            return -1;
        }

        private static string NormalizeHeader(string value) =>
            new(value.Trim().TrimStart('\uFEFF').ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string Value(IReadOnlyList<string> values, int index) => values[index].Trim();

        private static string NullableValue(IReadOnlyList<string> values, int index)
        {
            var value = Value(values, index);
            return string.Equals(value, "Tom", StringComparison.OrdinalIgnoreCase) ? "" : value;
        }

        private static bool TryParseAmount(string value, out decimal amount)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
                || decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("da-DK"), out amount);
        }

        private static List<List<string>> ReadRecords(string content)
        {
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < content.Length; index++)
            {
                var character = content[index];
                if (inQuotes)
                {
                    if (character == '"' && index + 1 < content.Length && content[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else if (character == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                if (character == '"')
                {
                    inQuotes = true;
                }
                else if (character == ';')
                {
                    record.Add(field.ToString());
                    field.Clear();
                }
                else if (character == '\r' || character == '\n')
                {
                    record.Add(field.ToString());
                    field.Clear();
                    if (record.Count > 1 || !string.IsNullOrWhiteSpace(record[0])) records.Add(record);
                    record = new List<string>();
                    if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n') index++;
                }
                else
                {
                    field.Append(character);
                }
            }

            if (inQuotes) throw new ArgumentException("CSV-filen indeholder et uafsluttet citationstegn.");

            if (field.Length > 0 || record.Count > 0)
            {
                record.Add(field.ToString());
                if (record.Count > 1 || !string.IsNullOrWhiteSpace(record[0])) records.Add(record);
            }

            return records;
        }
    }

    internal sealed class FinanceBudgetCsvRow
    {
        public int RowNumber { get; init; }
        public FinanceBudgetUpdateDto Update { get; init; } = new();
    }
}
