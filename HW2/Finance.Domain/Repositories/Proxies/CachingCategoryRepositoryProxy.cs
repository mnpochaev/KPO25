using System;
using System.Collections.Generic;
using System.Linq;

namespace Finance.Domain.Repositories.Proxies;

/// <summary>
/// Прокси для кэширования категорий в памяти, оборачивает основной репозиторий (Proxy pattern).
/// </summary>
public sealed class CachingCategoryRepositoryProxy : ICategoryRepository
{
    private readonly ICategoryRepository _inner;
    private IReadOnlyCollection<Category>? _cache;

    public CachingCategoryRepositoryProxy(ICategoryRepository inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public void Add(Category category)
    {
        _inner.Add(category);
        Invalidate();
    }

    public void Update(Category category)
    {
        _inner.Update(category);
        Invalidate();
    }

    public void Delete(Guid id)
    {
        _inner.Delete(id);
        Invalidate();
    }

    public Category? GetById(Guid id)
    {
        return GetAll().FirstOrDefault(c => c.Id == id);
    }

    public IReadOnlyCollection<Category> GetAll()
    {
        if (_cache is null)
        {
            _cache = _inner.GetAll();
        }

        return _cache;
    }

    private void Invalidate()
    {
        _cache = null;
    }
}
