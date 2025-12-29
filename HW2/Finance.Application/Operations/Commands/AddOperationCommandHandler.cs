using Finance.Application.Commands;
using Finance.Domain;

namespace Finance.Application.Operations.Commands;

/// <summary>
/// Команда добавления операции через фасад.
/// </summary>
public sealed class AddOperationCommandHandler : ICommandHandler<AddOperationCommand, Operation>
{
    private readonly OperationFacade _facade;

    public AddOperationCommandHandler(OperationFacade facade)
    {
        _facade = facade;
    }

    public string Name => "AddOperation";

    public Operation Handle(AddOperationCommand command)
    {
        return _facade.AddOperation(command);
    }
}
