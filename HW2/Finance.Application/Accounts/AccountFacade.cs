using System;
using System.Collections.Generic;
using System.Linq;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Application.Accounts;

/// <summary>
/// Фасад для работы со счетами (создание/обновление/удаление).
/// </summary>
public sealed class AccountFacade
{
    private readonly IBankAccountRepository _accounts;

    public AccountFacade(IBankAccountRepository accounts)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public BankAccount Create(string name, decimal initialBalance)
    {
        var account = new BankAccount(name, new Money(initialBalance));
        _accounts.Add(account);
        return account;
    }

    public void Rename(Guid id, string newName)
    {
        var account = _accounts.GetById(id) ?? throw new InvalidOperationException("Счет не найден");
        account.Rename(newName);
        _accounts.Update(account);
    }

    public void Delete(Guid id) => _accounts.Delete(id);

    public IReadOnlyCollection<BankAccount> List() => _accounts.GetAll().ToList();
}
