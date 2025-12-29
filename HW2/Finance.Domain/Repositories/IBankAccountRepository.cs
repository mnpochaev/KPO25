using System;
using System.Collections.Generic;

namespace Finance.Domain.Repositories;

/// <summary>
/// Репозиторий банковских счетов.
/// </summary>
public interface IBankAccountRepository
{
    void Add(BankAccount account);
    void Update(BankAccount account);
    void Delete(Guid id);
    BankAccount? GetById(Guid id);
    IReadOnlyCollection<BankAccount> GetAll();
}
