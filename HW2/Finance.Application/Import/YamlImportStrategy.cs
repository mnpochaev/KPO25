using System;
using System.Collections.Generic;
using System.Linq;

namespace Finance.Application.Import;

/// <summary>
/// Примитивная YAML-стратегия импорта (под формат, который генерирует наш экспорт).
/// </summary>
public sealed class YamlImportStrategy : ImportStrategyBase
{
    public override DataFormat Format => DataFormat.Yaml;

    protected override IEnumerable<ImportRowData?> ReadRaw(string data)
    {
        // ожидаем формат operations: - ключ: значение
        var lines = data.Split('\n').Select(l => l.Trim()).ToList();
        var current = new Dictionary<string, string?>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Equals("operations:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("-"))
            {
                if (current.Count > 0)
                {
                    yield return Build(current);
                    current.Clear();
                }

                var parts = line.TrimStart('-').Split(':', 2);
                if (parts.Length == 2)
                {
                    current[parts[0].Trim()] = parts[1].Trim();
                }
                continue;
            }

            var pair = line.Split(':', 2);
            if (pair.Length == 2)
            {
                current[pair[0].Trim()] = pair[1].Trim();
            }
        }

        if (current.Count > 0)
        {
            yield return Build(current);
        }
    }

    private static ImportRowData Build(Dictionary<string, string?> map)
    {
        map.TryGetValue("дата", out var date);
        map.TryGetValue("тип", out var type);
        map.TryGetValue("категория", out var category);
        map.TryGetValue("сумма", out var amountText);
        map.TryGetValue("валюта", out var currency);
        map.TryGetValue("описание", out var description);
        map.TryGetValue("счет", out var account);

        decimal.TryParse(amountText, out var amount);
        return new ImportRowData(account ?? string.Empty, category ?? string.Empty, type ?? string.Empty, amount, currency ?? "RUB", date ?? string.Empty, description);
    }
}
