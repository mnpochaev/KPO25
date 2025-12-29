using System.Collections.Generic;
using Finance.Domain;

namespace Finance.Application.Export;

/// <summary>
/// Контекст данных для экспорта (список операций + справочники).
/// </summary>
public sealed class ExportContext : IExportVisitable
{
    public IReadOnlyCollection<Operation> Operations { get; }

    public ExportContext(IReadOnlyCollection<Operation> operations)
    {
        Operations = operations;
    }

    public string Accept(IExportVisitor visitor)
    {
        return visitor.Visit(this);
    }
}
