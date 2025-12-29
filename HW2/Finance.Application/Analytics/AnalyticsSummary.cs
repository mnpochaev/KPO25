namespace Finance.Application.Analytics;

/// <summary>
/// Итоговые показатели аналитики по транзакциям.
/// </summary>
public sealed record AnalyticsSummary(decimal Income, decimal Expense)
{
    public decimal Balance => Income - Expense;
}
