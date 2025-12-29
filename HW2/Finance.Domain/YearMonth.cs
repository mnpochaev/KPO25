using System;

namespace Finance.Domain;

/// <summary>
/// Представление месяца (год + номер месяца).
/// </summary>
public sealed class YearMonth : IEquatable<YearMonth>
{
    public int Year { get; }
    public int Month { get; }

    public YearMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentException("Месяц должен быть в диапазоне 1..12", nameof(month));
        }

        Year = year;
        Month = month;
    }

    public static YearMonth FromDate(DateOnly date) => new(date.Year, date.Month);

    public bool Contains(DateOnly date) => date.Year == Year && date.Month == Month;

    public bool Equals(YearMonth? other)
    {
        if (other is null)
        {
            return false;
        }

        return Year == other.Year && Month == other.Month;
    }

    public override bool Equals(object? obj) => Equals(obj as YearMonth);

    public override int GetHashCode() => HashCode.Combine(Year, Month);

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
