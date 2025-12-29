using System;
using System.Collections.Generic;
using System.Linq;
using Finance.Domain;
using Finance.Domain.Repositories;
using Finance.Domain.Repositories.Proxies;

namespace Finance.Application.Categories;

/// <summary>
/// Фасад для работы с категориями (создание/редактирование/удаление).
/// </summary>
public sealed class CategoryFacade
{
    private readonly ICategoryRepository _categories;

    public CategoryFacade(ICategoryRepository categories)
    {
        // оборачиваем прокси кэша, чтобы продемонстрировать Proxy
        _categories = new CachingCategoryRepositoryProxy(categories ?? throw new ArgumentNullException(nameof(categories)));
    }

    public Category Create(string name, OperationType type)
    {
        var category = new Category(name, type);
        _categories.Add(category);
        return category;
    }

    public void Rename(Guid id, string newName)
    {
        var category = _categories.GetById(id) ?? throw new InvalidOperationException("Категория не найдена");
        var updated = new Category(id, newName, category.Kind);
        _categories.Update(updated);
    }

    public void Delete(Guid id) => _categories.Delete(id);

    public IReadOnlyCollection<Category> List() => _categories.GetAll().ToList();

    public Category GetOrCreate(string name, OperationType type)
    {
        var existing = _categories.GetAll().FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.Kind == type);
        return existing ?? Create(name, type);
    }
}
