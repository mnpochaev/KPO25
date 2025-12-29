using System;

namespace Finance.Domain;

/// <summary>
/// Банковский счет пользователя.
/// </summary>
public sealed class BankAccount : IEquatable<BankAccount>
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public Money Balance { get; private set; }

    public BankAccount(string name, Money balance) : this(Guid.NewGuid(), name, balance)
    {
    }

    public BankAccount(Guid id, string name, Money balance)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id счета не может быть пустым", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название счета должно быть заполнено", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Balance = balance ?? throw new ArgumentNullException(nameof(balance));
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Название счета должно быть заполнено", nameof(newName));
        }

        Name = newName.Trim();
    }

    public void SetBalance(Money money)
    {
        Balance = money ?? throw new ArgumentNullException(nameof(money));
    }

    public bool Equals(BankAccount? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as BankAccount);

    public override int GetHashCode() => Id.GetHashCode();
}
