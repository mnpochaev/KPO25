using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Finance.Domain;

namespace Finance.Application.Export.Visitors;

/// <summary>
/// JSON Visitor для экспорта.
/// </summary>
public sealed class JsonExportVisitor : IExportVisitor
{
    public DataFormat Format => DataFormat.Json;

    public string Visit(ExportContext context)
    {
        var list = new List<OperationDto>();
        foreach (var operation in context.Operations)
        {
            list.Add(new OperationDto
            {
                Date = operation.Date,
                AccountId = operation.BankAccountId,
                Type = operation.Type,
                Category = operation.Category.Name,
                Amount = operation.Money.Amount,
                Currency = operation.Money.Currency.ToString().ToUpperInvariant(),
                Description = operation.Description?.ToString()
            });
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Serialize(list, options);
    }

    private sealed class OperationDto
    {
        [JsonPropertyName("дата")]
        public DateOnly Date { get; set; }

        [JsonPropertyName("счет")]
        public Guid AccountId { get; set; }

        [JsonPropertyName("тип")]
        public OperationType Type { get; set; }

        [JsonPropertyName("категория")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("сумма")]
        public decimal Amount { get; set; }

        [JsonPropertyName("валюта")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("описание")]
        public string? Description { get; set; }
    }
}
