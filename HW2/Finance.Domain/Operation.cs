using System;

namespace Finance.Domain;

/// <summary>
/// Операция (доход или расход).
/// </summary>
public sealed class Operation
{
    public Guid Id { get; }
    public Guid BankAccountId { get; }
    public Guid CategoryId => Category.Id;
    public DateOnly Date { get; }
    public Money Money { get; }
    public Category Category { get; }
    public OperationType Type { get; }
    public OperationDescription? Description { get; }

    internal Operation(Guid id, Guid bankAccountId, Category category, DateOnly date, Money money, OperationType type, OperationDescription? description)
    {
        Id = id;
        BankAccountId = bankAccountId;
        Date = date;
        Money = money ?? throw new ArgumentNullException(nameof(money));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Type = type;
        Description = description;
    }
}
