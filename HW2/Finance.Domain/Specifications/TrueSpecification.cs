namespace Finance.Domain.Specifications;

/// <summary>
/// Спецификация, которая всегда истинна (удобна как стартовый элемент).
/// </summary>
public sealed class TrueSpecification<T> : Specification<T>
{
    public override bool IsSatisfiedBy(T item) => true;
}
