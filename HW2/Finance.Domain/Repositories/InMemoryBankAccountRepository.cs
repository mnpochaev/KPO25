using System;
using System.Collections.Generic;
using System.Linq;

namespace Finance.Domain.Repositories;

/// <summary>
/// Памятный репозиторий банковских счетов.
/// </summary>
public sealed class InMemoryBankAccountRepository : IBankAccountRepository
{
    private readonly List<BankAccount> _storage = new();

    public void Add(BankAccount account)
    {
        _storage.Add(account ?? throw new ArgumentNullException(nameof(account)));
    }

    public void Update(BankAccount account)
    {
        if (GetById(account.Id) is null)
        {
            throw new InvalidOperationException("Счет не найден");
        }

        Delete(account.Id);
        Add(account);
    }

    public void Delete(Guid id)
    {
        _storage.RemoveAll(a => a.Id == id);
    }

    public BankAccount? GetById(Guid id) => _storage.FirstOrDefault(a => a.Id == id);

    public IReadOnlyCollection<BankAccount> GetAll() => _storage.ToList();
}
