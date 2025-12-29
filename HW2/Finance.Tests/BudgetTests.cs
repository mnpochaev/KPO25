using Finance.Application.Accounts;
using Finance.Application.Budgets;
using Finance.Application.Notifications;
using Finance.Application.Operations;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Tests;

public class BudgetTests
{
    [Fact]
    public void При_превышении_лимита_отправляется_уведомление()
    {
        var operationRepo = new InMemoryOperationRepository();
        var accountRepo = new InMemoryBankAccountRepository();
        var categoryRepo = new InMemoryCategoryRepository();
        var budgetRepo = new InMemoryBudgetRepository();
        var factory = new OperationFactory();
        var balance = new BalanceService(accountRepo, operationRepo);
        var budgetService = new BudgetService(budgetRepo, operationRepo);
        var sink = new InMemoryNotificationSink();
        var notifier = new BudgetExceededNotifier(budgetService, sink, categoryRepo);
        var facade = new OperationFacade(operationRepo, accountRepo, categoryRepo, factory, balance, new[] { notifier });

        var account = new BankAccount("Основной", new Money(0));
        accountRepo.Add(account);
        var cafe = new Category("Кафе", OperationType.Expense);
        categoryRepo.Add(cafe);
        budgetService.SetMonthlyBudget(cafe, new Money(500), new YearMonth(2024, 3));

        facade.AddOperation(new AddOperationCommand(account.Id, cafe.Id, 300, Currency.Rub, OperationType.Expense, new DateOnly(2024, 3, 5), null));
        Assert.Empty(sink.Messages);

        facade.AddOperation(new AddOperationCommand(account.Id, cafe.Id, 250, Currency.Rub, OperationType.Expense, new DateOnly(2024, 3, 10), null));

        Assert.Single(sink.Messages);
        Assert.Contains("Превышен бюджет", sink.Messages.First());
    }
}
