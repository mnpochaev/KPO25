namespace Finance.Application.Commands;

/// <summary>
/// Обработчик сценария (Command pattern).
/// </summary>
public interface ICommandHandler<TCommand, TResult>
{
    string Name { get; }
    TResult Handle(TCommand command);
}
