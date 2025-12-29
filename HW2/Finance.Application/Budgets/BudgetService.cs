using System;
using System.Linq;
using Finance.Domain;
using Finance.Domain.Repositories;
using Finance.Domain.Specifications;

namespace Finance.Application.Budgets;

/// <summary>
/// Сервис работы с бюджетами.
/// </summary>
public sealed class BudgetService
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IOperationRepository _transactionRepository;

    public BudgetService(IBudgetRepository budgetRepository, IOperationRepository transactionRepository)
    {
        _budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public Budget SetMonthlyBudget(Category category, Money limit, YearMonth month)
    {
        var budget = new Budget(category, limit, month);
        _budgetRepository.Upsert(budget);
        return budget;
    }

    public Budget? GetBudget(Category category, YearMonth month) => _budgetRepository.Get(category, month);

    public Money CalculateSpent(Category category, YearMonth month)
    {
        var from = new DateOnly(month.Year, month.Month, 1);
        var to = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
        var spec = new OperationTypeSpecification(OperationType.Expense)
            .And(new CategorySpecification(category))
            .And(new DateRangeSpecification(new DateRange(from, to)));

        var spent = _transactionRepository.Query(spec).Sum(t => t.Money.Amount);
        return new Money(spent, ResolveCurrency());
    }

    public bool IsExceeded(Category category, YearMonth month)
    {
        var budget = _budgetRepository.Get(category, month);
        if (budget is null)
        {
            return false;
        }

        var spent = CalculateSpent(category, month);
        return budget.IsExceeded(spent);
    }

    private static Currency ResolveCurrency() => Currency.Rub; // при расширении можно хранить в бюджете
}
