using System;
using Finance.Application.Budgets;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Application.Notifications;

/// <summary>
/// Observer, реагирующий на расходы и отправляющий уведомление при превышении бюджета.
/// </summary>
public sealed class BudgetExceededNotifier : IOperationObserver
{
    private readonly BudgetService _budgetService;
    private readonly INotificationSink _notificationSink;
    private readonly ICategoryRepository _categories;

    public BudgetExceededNotifier(BudgetService budgetService, INotificationSink notificationSink, ICategoryRepository categories)
    {
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _notificationSink = notificationSink ?? throw new ArgumentNullException(nameof(notificationSink));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
    }

    public void OnOperationAdded(Operation transaction)
    {
        if (transaction.Type != OperationType.Expense)
        {
            return;
        }

        var month = YearMonth.FromDate(transaction.Date);
        var category = _categories.GetById(transaction.CategoryId);
        if (category is null)
        {
            return;
        }

        var budget = _budgetService.GetBudget(category, month);
        if (budget is null)
        {
            return;
        }

        var spent = _budgetService.CalculateSpent(category, month);
        if (budget.IsExceeded(spent))
        {
            var message = $"Превышен бюджет по категории '{category.Name}' за {month}: лимит {budget.MonthlyLimit.Amount}, расходы {spent.Amount}";
            _notificationSink.Notify(message);
        }
    }
}
