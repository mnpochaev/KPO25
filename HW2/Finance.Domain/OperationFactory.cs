using System;

namespace Finance.Domain;

/// <summary>
/// Фабрика (Factory Method) для создания операций с валидацией.
/// </summary>
public sealed class OperationFactory
{
    public Operation Create(OperationType type, Money money, BankAccount account, Category category, DateOnly? date = null, OperationDescription? description = null)
    {
        if (account is null)
        {
            throw new ArgumentNullException(nameof(account));
        }

        if (category is null)
        {
            throw new ArgumentNullException(nameof(category));
        }

        if (category.Kind != type)
        {
            throw new ArgumentException("Тип операции должен совпадать с типом категории", nameof(category));
        }

        var actualDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var id = Guid.NewGuid();
        return new Operation(id, account.Id, category, actualDate, money, type, description);
    }

    public Operation Update(Operation existing, OperationType type, Money money, BankAccount account, Category category, DateOnly date, OperationDescription? description = null)
    {
        if (existing is null)
        {
            throw new ArgumentNullException(nameof(existing));
        }

        if (category.Kind != type)
        {
            throw new ArgumentException("Тип операции должен совпадать с типом категории", nameof(category));
        }

        return new Operation(existing.Id, account.Id, category, date, money, type, description);
    }
}
