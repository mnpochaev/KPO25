using System.Collections.Generic;
using System.Linq;

namespace Finance.Application.Metrics;

/// <summary>
/// Памятное хранилище метрик.
/// </summary>
public sealed class InMemoryScenarioMetrics : IScenarioMetrics
{
    private readonly Dictionary<string, List<long>> _storage = new();

    public IReadOnlyDictionary<string, List<long>> All => _storage.ToDictionary(k => k.Key, v => v.Value.ToList());

    public void Add(string scenario, long milliseconds)
    {
        if (!_storage.TryGetValue(scenario, out var list))
        {
            list = new List<long>();
            _storage[scenario] = list;
        }

        list.Add(milliseconds);
    }
}
