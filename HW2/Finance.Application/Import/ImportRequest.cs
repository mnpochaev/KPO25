namespace Finance.Application.Import;

/// <summary>
/// Запрос импорта.
/// </summary>
public sealed record ImportRequest(string Data, DataFormat Format);
