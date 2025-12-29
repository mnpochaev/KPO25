using Finance.Application.Commands;

namespace Finance.Application.Operations.Commands;

/// <summary>
/// Команда удаления операции.
/// </summary>
public sealed class DeleteOperationCommandHandler : ICommandHandler<DeleteOperationCommand, bool>
{
    private readonly OperationFacade _facade;

    public DeleteOperationCommandHandler(OperationFacade facade)
    {
        _facade = facade;
    }

    public string Name => "DeleteOperation";

    public bool Handle(DeleteOperationCommand command)
    {
        _facade.Delete(command.Id);
        return true;
    }
}
