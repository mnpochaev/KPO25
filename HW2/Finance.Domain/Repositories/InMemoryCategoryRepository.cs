using System;
using System.Collections.Generic;
using System.Linq;

namespace Finance.Domain.Repositories;

/// <summary>
/// Памятный репозиторий категорий.
/// </summary>
public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _storage = new();

    public void Add(Category category)
    {
        _storage.Add(category ?? throw new ArgumentNullException(nameof(category)));
    }

    public void Update(Category category)
    {
        if (GetById(category.Id) is null)
        {
            throw new InvalidOperationException("Категория не найдена");
        }

        Delete(category.Id);
        Add(category);
    }

    public void Delete(Guid id)
    {
        _storage.RemoveAll(c => c.Id == id);
    }

    public Category? GetById(Guid id) => _storage.FirstOrDefault(c => c.Id == id);

    public IReadOnlyCollection<Category> GetAll() => _storage.ToList();
}
