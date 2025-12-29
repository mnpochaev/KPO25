using System;
using System.Collections.Generic;
using System.Globalization;

namespace Finance.Application.Import;

/// <summary>
/// Базовый шаблонный метод импорта (Template Method): общая обработка строк + форматный парсинг.
/// </summary>
public abstract class ImportStrategyBase : IImportStrategy
{
    public abstract DataFormat Format { get; }

    public IEnumerable<ParsedImportRow> Parse(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            yield break;
        }

        var index = 0;
        foreach (var raw in ReadRaw(data))
        {
            index++;
            if (raw is null)
            {
                yield return new ParsedImportRow(index, null, $"Строка {index}: нераспознанная запись");
                continue;
            }

            yield return Validate(raw, index);
        }
    }

    protected abstract IEnumerable<ImportRowData?> ReadRaw(string data);

    private ParsedImportRow Validate(ImportRowData raw, int line)
    {
        if (string.IsNullOrWhiteSpace(raw.AccountId))
        {
            return new ParsedImportRow(line, null, $"Строка {line}: не указан счет");
        }

        if (string.IsNullOrWhiteSpace(raw.CategoryName))
        {
            return new ParsedImportRow(line, null, $"Строка {line}: пустая категория");
        }

        if (raw.Amount < 0)
        {
            return new ParsedImportRow(line, null, $"Строка {line}: некорректная сумма");
        }

        if (!Enum.TryParse<Domain.OperationType>(raw.Type, true, out _))
        {
            return new ParsedImportRow(line, null, $"Строка {line}: неизвестный тип операции");
        }

        if (!DateOnly.TryParse(raw.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return new ParsedImportRow(line, null, $"Строка {line}: некорректная дата");
        }

        return new ParsedImportRow(line, raw, null);
    }
}
