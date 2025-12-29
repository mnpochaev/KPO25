using System;
using System.Collections.Generic;
using System.Linq;
using Finance.Application.Accounts;
using Finance.Application.Notifications;
using Finance.Application.Operations.Filters;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Application.Operations;

/// <summary>
/// Фасад use-case для управления операциями.
/// </summary>
public sealed class OperationFacade
{
    private readonly IOperationRepository _operations;
    private readonly IBankAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly OperationFactory _factory;
    private readonly IEnumerable<IOperationObserver> _observers;
    private readonly Accounts.BalanceService _balanceService;

    public OperationFacade(
        IOperationRepository operations,
        IBankAccountRepository accounts,
        ICategoryRepository categories,
        OperationFactory factory,
        Accounts.BalanceService balanceService,
        IEnumerable<IOperationObserver>? observers = null)
    {
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _balanceService = balanceService ?? throw new ArgumentNullException(nameof(balanceService));
        _observers = observers?.ToArray() ?? Array.Empty<IOperationObserver>();
    }

    public Operation AddOperation(AddOperationCommand command)
    {
        command = command ?? throw new ArgumentNullException(nameof(command));

        var account = _accounts.GetById(command.AccountId) ?? throw new InvalidOperationException("Счет не найден");
        var category = _categories.GetById(command.CategoryId) ?? throw new InvalidOperationException("Категория не найдена");

        var money = new Money(command.Amount, command.Currency);
        var description = string.IsNullOrWhiteSpace(command.Description) ? null : new OperationDescription(command.Description);
        var operation = _factory.Create(command.Type, money, account, category, command.Date, description);

        _operations.Add(operation);
        _balanceService.RecalculateAccount(account.Id);
        NotifyObservers(operation);
        return operation;
    }

    public Operation UpdateOperation(UpdateOperationCommand command)
    {
        command = command ?? throw new ArgumentNullException(nameof(command));
        var existing = _operations.GetById(command.Id) ?? throw new InvalidOperationException("Операция не найдена");
        var account = _accounts.GetById(command.AccountId) ?? throw new InvalidOperationException("Счет не найден");
        var category = _categories.GetById(command.CategoryId) ?? throw new InvalidOperationException("Категория не найдена");

        var money = new Money(command.Amount, command.Currency);
        var description = string.IsNullOrWhiteSpace(command.Description) ? null : new OperationDescription(command.Description);
        var updated = _factory.Update(existing, command.Type, money, account, category, command.Date, description);
        _operations.Update(updated);
        _balanceService.RecalculateAccount(account.Id);
        NotifyObservers(updated);
        return updated;
    }

    public void Delete(Guid id)
    {
        var existing = _operations.GetById(id);
        _operations.Delete(id);
        if (existing is not null)
        {
            _balanceService.RecalculateAccount(existing.BankAccountId);
        }
    }

    public IReadOnlyCollection<Operation> GetOperations(OperationQuery? query)
    {
        var builder = new OperationFilterBuilder();
        builder.WithType(query?.Type)
            .WithCategory(query?.CategoryId)
            .WithAccount(query?.AccountId)
            .WithinDateRange(query?.DateRange);

        var spec = builder.Build();
        return _operations.Query(spec)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    private void NotifyObservers(Operation operation)
    {
        foreach (var observer in _observers)
        {
            observer.OnOperationAdded(operation);
        }
    }
}
