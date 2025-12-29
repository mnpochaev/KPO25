using System;
using Finance.Domain;
using Finance.Domain.Specifications;

namespace Finance.Application.Operations.Filters;

/// <summary>
/// Построитель фильтров (Builder) для чтения операций.
/// </summary>
public sealed class OperationFilterBuilder
{
    private ISpecification<Operation> _specification = new TrueSpecification<Operation>();

    public OperationFilterBuilder WithType(OperationType? type)
    {
        if (type.HasValue)
        {
            _specification = _specification.And(new OperationTypeSpecification(type.Value));
        }

        return this;
    }

    public OperationFilterBuilder WithCategory(Guid? categoryId)
    {
        if (categoryId.HasValue)
        {
            _specification = _specification.And(new CategorySpecification(categoryId.Value));
        }

        return this;
    }

    public OperationFilterBuilder WithAccount(Guid? accountId)
    {
        if (accountId.HasValue)
        {
            _specification = _specification.And(new AccountSpecification(accountId.Value));
        }

        return this;
    }

    public OperationFilterBuilder WithinDateRange(DateRange? range)
    {
        if (range is not null)
        {
            _specification = _specification.And(new DateRangeSpecification(range));
        }

        return this;
    }

    public ISpecification<Operation> Build() => _specification;
}
