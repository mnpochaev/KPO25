using Finance.Application.Operations.Filters;

namespace Finance.Application.Export;

/// <summary>
/// Запрос экспорта.
/// </summary>
public sealed record ExportRequest(OperationQuery Query, DataFormat Format);
