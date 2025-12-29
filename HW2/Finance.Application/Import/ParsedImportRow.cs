namespace Finance.Application.Import;

/// <summary>
/// Результат парсинга строки импорта.
/// </summary>
public sealed record ParsedImportRow(int LineNumber, ImportRowData? Data, string? Error);

public sealed record ImportRowData(
    string AccountId,
    string CategoryName,
    string Type,
    decimal Amount,
    string Currency,
    string Date,
    string? Description);
