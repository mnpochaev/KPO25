using System.Collections.Generic;
using System.Linq;

namespace Finance.Application.Notifications;

/// <summary>
/// Памятный приемник уведомлений для тестов/демо.
/// </summary>
public sealed class InMemoryNotificationSink : INotificationSink
{
    private readonly List<string> _messages = new();

    public IReadOnlyCollection<string> Messages => _messages.ToList();

    public void Notify(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _messages.Add(message.Trim());
        }
    }
}
