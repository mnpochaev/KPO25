using Finance.Domain;

namespace Finance.Application.Notifications;

/// <summary>
/// Подписчик на событие добавления транзакции (Observer pattern).
/// </summary>
public interface IOperationObserver
{
    void OnOperationAdded(Operation transaction);
}
