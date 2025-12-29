using Finance.Application.Commands;
using Finance.Domain;

namespace Finance.Application.Operations.Commands;

/// <summary>
/// Команда обновления операции.
/// </summary>
public sealed class UpdateOperationCommandHandler : ICommandHandler<UpdateOperationCommand, Operation>
{
    private readonly OperationFacade _facade;

    public UpdateOperationCommandHandler(OperationFacade facade)
    {
        _facade = facade;
    }

    public string Name => "UpdateOperation";

    public Operation Handle(UpdateOperationCommand command)
    {
        return _facade.UpdateOperation(command);
    }
}
