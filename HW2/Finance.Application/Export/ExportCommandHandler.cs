using Finance.Application.Commands;

namespace Finance.Application.Export;

/// <summary>
/// Команда экспорта.
/// </summary>
public sealed class ExportCommandHandler : ICommandHandler<ExportRequest, string>
{
    private readonly IExportService _service;

    public ExportCommandHandler(IExportService service)
    {
        _service = service;
    }

    public string Name => "Export";

    public string Handle(ExportRequest command)
    {
        return _service.Export(command.Query, command.Format);
    }
}
