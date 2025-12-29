using System.Collections.Generic;

namespace Finance.Application.Import;

/// <summary>
/// Стратегия импорта данных.
/// </summary>
public interface IImportStrategy
{
    DataFormat Format { get; }

    IEnumerable<ParsedImportRow> Parse(string data);
}
