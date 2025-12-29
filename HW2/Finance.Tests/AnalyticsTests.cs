using Finance.Application.Accounts;
using Finance.Application.Analytics;
using Finance.Application.Categories;
using Finance.Application.Operations;
using Finance.Application.Operations.Filters;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Tests;

public class AnalyticsTests
{
    private readonly IBankAccountRepository _accounts = new InMemoryBankAccountRepository();
    private readonly ICategoryRepository _categories = new InMemoryCategoryRepository();
    private readonly IOperationRepository _operations = new InMemoryOperationRepository();

    private OperationFacade BuildFacade()
    {
        var factory = new OperationFactory();
        var balance = new BalanceService(_accounts, _operations);
        return new OperationFacade(_operations, _accounts, _categories, factory, balance, null);
    }

    [Fact]
    public void Баланс_считается_корректно()
    {
        var facade = BuildFacade();
        var analytics = new AnalyticsService(_operations);
        var account = new AccountFacade(_accounts).Create("Основной", 0);
        var salary = new Category("Зарплата", OperationType.Income);
        var cafe = new Category("Кафе", OperationType.Expense);
        _categories.Add(salary);
        _categories.Add(cafe);

        facade.AddOperation(new AddOperationCommand(account.Id, salary.Id, 1000, Currency.Rub, OperationType.Income, new DateOnly(2024, 2, 1), null));
        facade.AddOperation(new AddOperationCommand(account.Id, cafe.Id, 300, Currency.Rub, OperationType.Expense, new DateOnly(2024, 2, 2), null));

        var summary = analytics.GetSummary(new OperationQuery());

        Assert.Equal(700, summary.Balance);
        Assert.Equal(1000, summary.Income);
        Assert.Equal(300, summary.Expense);
    }

    [Fact]
    public void Распределение_расходов_по_категориям_работает()
    {
        var facade = BuildFacade();
        var analytics = new AnalyticsService(_operations);
        var account = new AccountFacade(_accounts).Create("Основной", 0);
        var cafe = new Category("Кафе", OperationType.Expense);
        var health = new Category("Здоровье", OperationType.Expense);
        _categories.Add(cafe);
        _categories.Add(health);

        facade.AddOperation(new AddOperationCommand(account.Id, cafe.Id, 200, Currency.Rub, OperationType.Expense, new DateOnly(2024, 3, 1), null));
        facade.AddOperation(new AddOperationCommand(account.Id, cafe.Id, 100, Currency.Rub, OperationType.Expense, new DateOnly(2024, 3, 2), null));
        facade.AddOperation(new AddOperationCommand(account.Id, health.Id, 400, Currency.Rub, OperationType.Expense, new DateOnly(2024, 3, 3), null));

        var distribution = analytics.GetExpenseByCategory(new OperationQuery());

        Assert.Equal(2, distribution.Count);
        Assert.Equal(300, distribution[cafe.Name]);
        Assert.Equal(400, distribution[health.Name]);
    }
}
