using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Finance.Application.Import;

/// <summary>
/// CSV-стратегия импорта (Template Method наследует базовый класс).
/// </summary>
public sealed class CsvImportStrategy : ImportStrategyBase
{
    public override DataFormat Format => DataFormat.Csv;

    protected override IEnumerable<ImportRowData?> ReadRaw(string data)
    {
        var lines = data.Split('\n');
        var lineNumber = 0;
        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.Trim('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (lineNumber == 1 && IsHeader(line))
            {
                continue;
            }

            var columns = SplitCsvLine(line);
            if (columns.Count < 6)
            {
                yield return null;
                continue;
            }

            var date = columns[0];
            var type = columns[1];
            var category = columns[2];
            var amountText = columns[3];
            var currency = columns[4];
            var description = columns.ElementAtOrDefault(5);
            var account = columns.Count > 6 ? columns[6] : string.Empty;

            if (!decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                yield return new ImportRowData(account, category, type, 0, currency, date, description);
                continue;
            }

            yield return new ImportRowData(account, category, type, amount, currency, date, description);
        }
    }

    private static bool IsHeader(string line) => line.StartsWith("Дата", StringComparison.OrdinalIgnoreCase);

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result.Select(v => v.Trim()).ToList();
    }
}
