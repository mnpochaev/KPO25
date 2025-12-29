namespace Finance.Domain.Specifications;

/// <summary>
/// Базовый интерфейс спецификации (Specification pattern) для фильтрации.
/// </summary>
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T item);

    ISpecification<T> And(ISpecification<T> other);
}
