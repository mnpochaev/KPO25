using System;

namespace Finance.Domain.Specifications;

/// <summary>
/// Фильтр транзакций по диапазону дат.
/// </summary>
public sealed class DateRangeSpecification : Specification<Operation>
{
    private readonly DateRange _range;

    public DateRangeSpecification(DateRange range)
    {
        _range = range ?? throw new ArgumentNullException(nameof(range));
    }

    public override bool IsSatisfiedBy(Operation item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return _range.Contains(item.Date);
    }
}
