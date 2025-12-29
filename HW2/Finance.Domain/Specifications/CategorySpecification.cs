using System;

namespace Finance.Domain.Specifications;

/// <summary>
/// Фильтр по категории.
/// </summary>
public sealed class CategorySpecification : Specification<Operation>
{
    private readonly Guid _categoryId;

    public CategorySpecification(Category category)
    {
        if (category is null)
        {
            throw new ArgumentNullException(nameof(category));
        }

        _categoryId = category.Id;
    }

    public CategorySpecification(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Id категории не может быть пустым", nameof(categoryId));
        }

        _categoryId = categoryId;
    }

    public override bool IsSatisfiedBy(Operation item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return item.CategoryId == _categoryId;
    }
}
