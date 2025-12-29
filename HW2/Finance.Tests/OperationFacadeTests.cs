using Finance.Application.Accounts;
using Finance.Application.Categories;
using Finance.Application.Operations;
using Finance.Application.Operations.Commands;
using Finance.Application.Operations.Filters;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Tests;

public class OperationFacadeTests
{
    private readonly IBankAccountRepository _accounts = new InMemoryBankAccountRepository();
    private readonly ICategoryRepository _categories = new InMemoryCategoryRepository();
    private readonly IOperationRepository _operations = new InMemoryOperationRepository();

    private OperationFacade BuildFacade()
    {
        var factory = new OperationFactory();
        var balanceService = new BalanceService(_accounts, _operations);
        return new OperationFacade(_operations, _accounts, _categories, factory, balanceService, null);
    }

    [Fact]
    public void Нельзя_создать_операцию_с_несовпадающим_типом()
    {
        var facade = BuildFacade();
        var account = new AccountFacade(_accounts).Create("Основной", 0);
        var category = new Category("Зарплата", OperationType.Income);
        _categories.Add(category);

        var cmd = new AddOperationCommand(account.Id, category.Id, 100, Currency.Rub, OperationType.Expense, null, null);
        Assert.Throws<ArgumentException>(() => facade.AddOperation(cmd));
    }

    [Fact]
    public void Добавленная_операция_обновляет_баланс_счета()
    {
        var facade = BuildFacade();
        var account = new AccountFacade(_accounts).Create("Основной", 0);
        var category = new Category("Зарплата", OperationType.Income);
        _categories.Add(category);

        var cmd = new AddOperationCommand(account.Id, category.Id, 1000, Currency.Rub, OperationType.Income, null, null);
        var op = facade.AddOperation(cmd);

        var updatedAccount = _accounts.GetById(account.Id)!;
        Assert.Equal(1000, updatedAccount.Balance.Amount);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), op.Date);
    }

    [Fact]
    public void Удаление_операции_пересчитывает_баланс()
    {
        var facade = BuildFacade();
        var account = new AccountFacade(_accounts).Create("Основной", 0);
        var category = new Category("Кафе", OperationType.Expense);
        _categories.Add(category);

        var add = new AddOperationCommand(account.Id, category.Id, 200, Currency.Rub, OperationType.Expense, new DateOnly(2024, 1, 1), null);
        var op = facade.AddOperation(add);
        Assert.Equal(-200, _accounts.GetById(account.Id)!.Balance.Amount);

        facade.Delete(op.Id);
        Assert.Equal(0, _accounts.GetById(account.Id)!.Balance.Amount);
    }

    [Fact]
    public void Фильтр_по_типу_и_категории_работает()
    {
        var facade = BuildFacade();
        var account = new AccountFacade(_accounts).Create("Основной", 0);
        var salary = new Category("Зарплата", OperationType.Income);
        var cafe = new Category("Кафе", OperationType.Expense);
        _categories.Add(salary);
        _categories.Add(cafe);

        facade.AddOperation(new AddOperationCommand(account.Id, salary.Id, 500, Currency.Rub, OperationType.Income, new DateOnly(2024, 1, 1), null));
        facade.AddOperation(new AddOperationCommand(account.Id, cafe.Id, 100, Currency.Rub, OperationType.Expense, new DateOnly(2024, 1, 2), null));

        var query = new OperationQuery { Type = OperationType.Expense, CategoryId = cafe.Id };
        var list = facade.GetOperations(query);

        Assert.Single(list);
        Assert.Equal(cafe.Id, list.First().CategoryId);
    }
}
