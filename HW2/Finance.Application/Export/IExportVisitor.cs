namespace Finance.Application.Export;

/// <summary>
/// Visitor для экспорта данных в разные форматы.
/// </summary>
public interface IExportVisitor
{
    DataFormat Format { get; }
    string Visit(ExportContext context);
}
