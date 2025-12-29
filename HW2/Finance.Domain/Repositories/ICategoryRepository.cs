using System;
using System.Collections.Generic;

namespace Finance.Domain.Repositories;

/// <summary>
/// Репозиторий категорий.
/// </summary>
public interface ICategoryRepository
{
    void Add(Category category);
    void Update(Category category);
    void Delete(Guid id);
    Category? GetById(Guid id);
    IReadOnlyCollection<Category> GetAll();
}
