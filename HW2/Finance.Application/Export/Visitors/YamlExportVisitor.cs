using System.Linq;
using System.Text;

namespace Finance.Application.Export.Visitors;

/// <summary>
/// Простой YAML Visitor (без внешних библиотек, минимальный формат).
/// </summary>
public sealed class YamlExportVisitor : IExportVisitor
{
    public DataFormat Format => DataFormat.Yaml;

    public string Visit(ExportContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("operations:");
        foreach (var operation in context.Operations)
        {
            sb.AppendLine("  - дата: " + operation.Date.ToString("yyyy-MM-dd"));
            sb.AppendLine("    счет: " + operation.BankAccountId);
            sb.AppendLine("    тип: " + operation.Type);
            sb.AppendLine("    категория: " + Escape(operation.Category.Name));
            sb.AppendLine("    сумма: " + operation.Money.Amount);
            sb.AppendLine("    валюта: " + operation.Money.Currency);
            if (!string.IsNullOrWhiteSpace(operation.Description?.ToString()))
            {
                sb.AppendLine("    описание: " + Escape(operation.Description!.ToString()!));
            }
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        return value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
    }
}
