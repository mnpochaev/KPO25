using System;

namespace Finance.Domain;

/// <summary>
/// Денежная сумма как объект-значение.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public static Money Zero(Currency currency = Currency.Rub) => new(0, currency);

    public Money(decimal amount, Currency currency = Currency.Rub, bool allowNegative = false)
    {
        if (!allowNegative && amount < 0)
        {
            throw new ArgumentException("Сумма не может быть отрицательной", nameof(amount));
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.ToZero);
        Currency = currency;
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        var result = Amount - other.Amount;
        if (result < 0)
        {
            throw new InvalidOperationException("Результат вычитания не может быть отрицательным для Money");
        }

        return new Money(result, Currency);
    }

    public bool Equals(Money? other)
    {
        if (other is null)
        {
            return false;
        }

        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount:F2} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Валюты должны совпадать для операции");
        }
    }
}
