using System;
using System.Collections.Generic;
using System.Linq;
using Finance.Domain;

namespace Finance.Domain.Repositories;

/// <summary>
/// Памятная реализация репозитория бюджетов.
/// </summary>
public sealed class InMemoryBudgetRepository : IBudgetRepository
{
    private readonly Dictionary<(Guid CategoryId, YearMonth Month), Budget> _storage = new();

    public void Upsert(Budget budget)
    {
        budget = budget ?? throw new ArgumentNullException(nameof(budget));
        var key = (budget.Category.Id, budget.Month);
        _storage[key] = budget;
    }

    public Budget? Get(Category category, YearMonth month)
    {
        category = category ?? throw new ArgumentNullException(nameof(category));
        month = month ?? throw new ArgumentNullException(nameof(month));
        var key = (category.Id, month);
        return _storage.TryGetValue(key, out var budget) ? budget : null;
    }

    public IReadOnlyCollection<Budget> GetAll() => _storage.Values.ToList();
}
