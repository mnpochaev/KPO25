using System;
using System.Linq;
using Finance.Domain;
using Finance.Domain.Repositories;
using Finance.Domain.Specifications;

namespace Finance.Application.Accounts;

/// <summary>
/// Сервис пересчета балансов счетов.
/// </summary>
public sealed class BalanceService
{
    private readonly IBankAccountRepository _accounts;
    private readonly IOperationRepository _operations;

    public BalanceService(IBankAccountRepository accounts, IOperationRepository operations)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    public void RecalculateAccount(Guid accountId)
    {
        var account = _accounts.GetById(accountId) ?? throw new InvalidOperationException("Счет не найден");
        var spec = new AccountSpecification(accountId);
        var operations = _operations.Query(spec);
        var income = operations.Where(o => o.Type == OperationType.Income).Sum(o => o.Money.Amount);
        var expense = operations.Where(o => o.Type == OperationType.Expense).Sum(o => o.Money.Amount);
        var newBalance = income - expense;
        account.SetBalance(new Money(newBalance, account.Balance.Currency, allowNegative: true));
        _accounts.Update(account);
    }

    public void RecalculateAll()
    {
        foreach (var account in _accounts.GetAll())
        {
            RecalculateAccount(account.Id);
        }
    }
}
