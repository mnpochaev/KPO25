using System;
using Finance.Domain;

namespace Finance.Application.Operations.Filters;

/// <summary>
/// Параметры фильтрации списка операций.
/// </summary>
public sealed class OperationQuery
{
    public OperationType? Type { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? AccountId { get; init; }
    public DateRange? DateRange { get; init; }
}
