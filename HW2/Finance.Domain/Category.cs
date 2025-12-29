using System;

namespace Finance.Domain;

/// <summary>
/// Категория операции.
/// </summary>
public sealed class Category : IEquatable<Category>
{
    public Guid Id { get; }
    public string Name { get; }
    public OperationType Kind { get; }

    public Category(string name, OperationType kind) : this(Guid.NewGuid(), name, kind)
    {
    }

    public Category(Guid id, string name, OperationType kind)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id категории не может быть пустым", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название категории должно быть заполнено", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Kind = kind;
    }

    public bool Equals(Category? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as Category);

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => $"{Name} ({Kind})";
}
