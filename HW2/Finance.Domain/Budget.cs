using System;

namespace Finance.Domain;

/// <summary>
/// Месячный бюджет по категории расходов.
/// </summary>
public sealed class Budget
{
    public Category Category { get; }
    public Money MonthlyLimit { get; }
    public YearMonth Month { get; }

    public Budget(Category category, Money monthlyLimit, YearMonth month)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        if (category.Kind != OperationType.Expense)
        {
            throw new ArgumentException("Бюджет разрешен только для расходов", nameof(category));
        }

        MonthlyLimit = monthlyLimit ?? throw new ArgumentNullException(nameof(monthlyLimit));
        Month = month ?? throw new ArgumentNullException(nameof(month));
    }

    public bool IsExceeded(Money spent)
    {
        if (spent is null)
        {
            throw new ArgumentNullException(nameof(spent));
        }

        if (spent.Currency != MonthlyLimit.Currency)
        {
            throw new InvalidOperationException("Валюты бюджета и траты должны совпадать");
        }

        return spent.Amount > MonthlyLimit.Amount;
    }
}
