using System;
using System.Collections.Generic;
using System.Linq;

namespace Finance.Application.Export;

/// <summary>
/// Фабрика выбора Export Visitor по формату.
/// </summary>
public sealed class ExportVisitorFactory
{
    private readonly IReadOnlyDictionary<DataFormat, IExportVisitor> _visitors;

    public ExportVisitorFactory(IEnumerable<IExportVisitor> visitors)
    {
        _visitors = visitors?.ToDictionary(v => v.Format) ?? throw new ArgumentNullException(nameof(visitors));
    }

    public IExportVisitor Resolve(DataFormat format)
    {
        if (!_visitors.TryGetValue(format, out var visitor))
        {
            throw new ArgumentException($"Нет посетителя для формата {format}", nameof(format));
        }

        return visitor;
    }
}
