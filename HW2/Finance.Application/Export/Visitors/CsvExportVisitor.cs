using System.Globalization;
using System.Linq;
using System.Text;
using Finance.Domain;

namespace Finance.Application.Export.Visitors;

/// <summary>
/// CSV Visitor (Visitor pattern) для экспорта.
/// </summary>
public sealed class CsvExportVisitor : IExportVisitor
{
    public DataFormat Format => DataFormat.Csv;

    public string Visit(ExportContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Дата,Счет,Тип,Категория,Сумма,Валюта,Описание");

        foreach (var operation in context.Operations)
        {
            var description = operation.Description?.ToString() ?? string.Empty;
            var line = string.Join(',', new[]
            {
                operation.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                operation.BankAccountId.ToString(),
                ToRussianType(operation.Type),
                Escape(operation.Category.Name),
                operation.Money.Amount.ToString(CultureInfo.InvariantCulture),
                operation.Money.Currency.ToString().ToUpperInvariant(),
                Escape(description)
            });
            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    private static string ToRussianType(OperationType type) => type == OperationType.Income ? "Доход" : "Расход";

    private static string Escape(string value)
    {
        if (value.Contains(','))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
