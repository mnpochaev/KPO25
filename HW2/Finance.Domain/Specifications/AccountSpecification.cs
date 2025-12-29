using System;

namespace Finance.Domain.Specifications;

/// <summary>
/// Фильтр операций по счёту.
/// </summary>
public sealed class AccountSpecification : Specification<Operation>
{
    private readonly Guid _accountId;

    public AccountSpecification(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Id счета не может быть пустым", nameof(accountId));
        }

        _accountId = accountId;
    }

    public override bool IsSatisfiedBy(Operation item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return item.BankAccountId == _accountId;
    }
}
