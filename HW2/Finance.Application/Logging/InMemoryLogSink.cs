using System.Collections.Generic;
using System.Linq;

namespace Finance.Application.Logging;

/// <summary>
/// Памятная реализация логов, удобна для тестов.
/// </summary>
public sealed class InMemoryLogSink : ILogSink
{
    private readonly List<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries.ToList();

    public void Log(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _entries.Add(message.Trim());
        }
    }
}
