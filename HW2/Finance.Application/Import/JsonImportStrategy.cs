using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Finance.Application.Import;

/// <summary>
/// JSON-стратегия импорта.
/// </summary>
public sealed class JsonImportStrategy : ImportStrategyBase
{
    public override DataFormat Format => DataFormat.Json;

    protected override IEnumerable<ImportRowData?> ReadRaw(string data)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(data);
        }
        catch
        {
            yield break;
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var date = GetString(element, "дата", "date") ?? string.Empty;
            var type = GetString(element, "тип", "type") ?? string.Empty;
            var category = GetString(element, "категория", "category") ?? string.Empty;
            var currency = GetString(element, "валюта", "currency") ?? "RUB";
            var account = GetString(element, "счет", "account") ?? string.Empty;
            var description = GetString(element, "описание", "description");
            var amount = GetDecimal(element, "сумма", "amount");
            yield return new ImportRowData(account, category, type, amount, currency, date, description);
        }
    }

    private static string? GetString(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop1))
        {
            return prop1.GetString();
        }

        if (element.TryGetProperty(name2, out var prop2))
        {
            return prop2.GetString();
        }

        return null;
    }

    private static decimal GetDecimal(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop1) && prop1.TryGetDecimal(out var v1))
        {
            return v1;
        }

        if (element.TryGetProperty(name2, out var prop2) && prop2.TryGetDecimal(out var v2))
        {
            return v2;
        }

        return 0;
    }
}
