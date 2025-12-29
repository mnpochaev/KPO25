using System.Collections.Generic;

namespace Finance.Application.Notifications;

/// <summary>
/// Простой интерфейс доставки уведомлений.
/// </summary>
public interface INotificationSink
{
    void Notify(string message);

    IReadOnlyCollection<string> Messages { get; }
}
