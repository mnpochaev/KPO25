using System;

namespace Finance.Domain;

/// <summary>
/// Диапазон дат.
/// </summary>
public sealed class DateRange : IEquatable<DateRange>
{
    public DateOnly From { get; }
    public DateOnly To { get; }

    public DateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("Дата начала не может быть позже даты окончания", nameof(from));
        }

        From = from;
        To = to;
    }

    public bool Contains(DateOnly date) => date >= From && date <= To;

    public bool Equals(DateRange? other)
    {
        if (other is null)
        {
            return false;
        }

        return From == other.From && To == other.To;
    }

    public override bool Equals(object? obj) => Equals(obj as DateRange);

    public override int GetHashCode() => HashCode.Combine(From, To);
}
