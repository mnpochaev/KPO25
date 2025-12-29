using System;
using System.Collections.Generic;
using System.Linq;
using Finance.Domain.Specifications;
using Finance.Domain;

namespace Finance.Domain.Repositories;

/// <summary>
/// Памятная реализация репозитория операций.
/// </summary>
public sealed class InMemoryOperationRepository : IOperationRepository
{
    private readonly List<Operation> _storage = new();

    public void Add(Operation operation)
    {
        _storage.Add(operation ?? throw new ArgumentNullException(nameof(operation)));
    }

    public void Update(Operation operation)
    {
        var existing = GetById(operation.Id);
        if (existing is null)
        {
            throw new InvalidOperationException("Операция не найдена");
        }

        Delete(operation.Id);
        Add(operation);
    }

    public void Delete(Guid id)
    {
        _storage.RemoveAll(t => t.Id == id);
    }

    public Operation? GetById(Guid id)
    {
        return _storage.FirstOrDefault(t => t.Id == id);
    }

    public IReadOnlyCollection<Operation> GetAll() => _storage.ToList();

    public IEnumerable<Operation> Query(ISpecification<Operation> specification)
    {
        specification ??= new TrueSpecification<Operation>();
        return _storage.Where(specification.IsSatisfiedBy).ToList();
    }
}
