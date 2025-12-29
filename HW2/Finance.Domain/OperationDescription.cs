using System;

namespace Finance.Domain;

/// <summary>
/// Описание транзакции с ограничением длины.
/// </summary>
public sealed class OperationDescription : IEquatable<OperationDescription>
{
    public const int MaxLength = 250;

    public string Value { get; }

    public OperationDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Описание не может быть пустым", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Описание не должно превышать {MaxLength} символов", nameof(value));
        }

        Value = trimmed;
    }

    public bool Equals(OperationDescription? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as OperationDescription);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
