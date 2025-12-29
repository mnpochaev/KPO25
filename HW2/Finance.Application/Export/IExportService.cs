using Finance.Application.Operations.Filters;

namespace Finance.Application.Export;

/// <summary>
/// Контракт сервиса экспорта.
/// </summary>
public interface IExportService
{
    string Export(OperationQuery? query, DataFormat format);
}
