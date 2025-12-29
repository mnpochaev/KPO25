using System;

namespace Finance.Domain.Specifications;

/// <summary>
/// Фильтр по типу транзакции.
/// </summary>
public sealed class OperationTypeSpecification : Specification<Operation>
{
    private readonly OperationType _type;

    public OperationTypeSpecification(OperationType type)
    {
        _type = type;
    }

    public override bool IsSatisfiedBy(Operation item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return item.Type == _type;
    }
}
