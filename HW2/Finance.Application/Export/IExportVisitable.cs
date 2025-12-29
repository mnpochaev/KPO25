namespace Finance.Application.Export;

/// <summary>
/// Объект, принимающий экспортирующего посетителя.
/// </summary>
public interface IExportVisitable
{
    string Accept(IExportVisitor visitor);
}
