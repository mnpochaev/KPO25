using System.Collections.Generic;

namespace Finance.Domain.Repositories;

/// <summary>
/// Контракт репозитория бюджетов.
/// </summary>
public interface IBudgetRepository
{
    void Upsert(Budget budget);

    Budget? Get(Category category, YearMonth month);

    IReadOnlyCollection<Budget> GetAll();
}
