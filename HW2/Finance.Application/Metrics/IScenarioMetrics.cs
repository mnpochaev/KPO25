using System.Collections.Generic;

namespace Finance.Application.Metrics;

/// <summary>
/// Хранилище метрик сценариев.
/// </summary>
public interface IScenarioMetrics
{
    void Add(string scenario, long milliseconds);
    IReadOnlyDictionary<string, List<long>> All { get; }
}
