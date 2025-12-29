using System.Collections.Generic;
using System.Linq;

namespace Finance.Application.Import;

/// <summary>
/// Результат импорта: сколько добавлено и какие ошибки.
/// </summary>
public sealed record ImportResult(int AddedCount, IReadOnlyCollection<string> Errors)
{
    public static ImportResult From(int added, IEnumerable<string> errors)
    {
        return new ImportResult(added, errors?.ToList() ?? new List<string>());
    }
}
