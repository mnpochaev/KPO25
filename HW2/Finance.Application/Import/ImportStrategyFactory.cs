using System;
using System.Collections.Generic;
using System.Linq;

namespace Finance.Application.Import;

/// <summary>
/// Реестр стратегий импорта, применяющий фабричный метод.
/// </summary>
public sealed class ImportStrategyFactory : IImportStrategyFactory
{
    private readonly IReadOnlyDictionary<DataFormat, IImportStrategy> _strategies;

    public ImportStrategyFactory(IEnumerable<IImportStrategy> strategies)
    {
        _strategies = strategies?.ToDictionary(s => s.Format) ?? throw new ArgumentNullException(nameof(strategies));
    }

    public IImportStrategy Resolve(DataFormat format)
    {
        if (!_strategies.TryGetValue(format, out var strategy))
        {
            throw new ArgumentException($"Стратегия импорта {format} не найдена", nameof(format));
        }

        return strategy;
    }
}
