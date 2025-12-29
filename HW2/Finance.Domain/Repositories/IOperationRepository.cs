using System;
using System.Collections.Generic;
using Finance.Domain.Specifications;

namespace Finance.Domain.Repositories;

/// <summary>
/// Контракт репозитория операций.
/// </summary>
public interface IOperationRepository
{
    void Add(Operation operation);

    void Update(Operation operation);

    void Delete(Guid id);

    Operation? GetById(Guid id);

    IReadOnlyCollection<Operation> GetAll();

    IEnumerable<Operation> Query(ISpecification<Operation> specification);
}
