using Finance.Application.Commands;

namespace Finance.Application.Import;

/// <summary>
/// Команда импорта.
/// </summary>
public sealed class ImportCommandHandler : ICommandHandler<ImportRequest, ImportResult>
{
    private readonly ImportService _service;

    public ImportCommandHandler(ImportService service)
    {
        _service = service;
    }

    public string Name => "Import";

    public ImportResult Handle(ImportRequest command)
    {
        return _service.Import(command.Data, command.Format);
    }
}
