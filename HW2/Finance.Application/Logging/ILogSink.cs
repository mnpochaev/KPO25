using System.Collections.Generic;

namespace Finance.Application.Logging;

/// <summary>
/// Приемник логов для декораторов.
/// </summary>
public interface ILogSink
{
    void Log(string message);

    IReadOnlyCollection<string> Entries { get; }
}
