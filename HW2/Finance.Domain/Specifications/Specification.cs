namespace Finance.Domain.Specifications;

/// <summary>
/// Базовая спецификация с поддержкой композиции.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T item);

    public ISpecification<T> And(ISpecification<T> other)
    {
        return new AndSpecification<T>(this, other);
    }
}
