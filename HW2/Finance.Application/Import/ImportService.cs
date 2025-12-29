using System;
using System.Collections.Generic;
using Finance.Application.Categories;
using Finance.Application.Operations;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Application.Import;

/// <summary>
/// Сервис импорта, выбирает стратегию и добавляет операции (Template Method + Strategy).
/// </summary>
public sealed class ImportService
{
    private readonly OperationFacade _operationFacade;
    private readonly IImportStrategyFactory _strategyFactory;
    private readonly CategoryFacade _categoryFacade;
    private readonly IBankAccountRepository _accounts;

    public ImportService(OperationFacade operationFacade, IImportStrategyFactory strategyFactory, CategoryFacade categoryFacade, IBankAccountRepository accounts)
    {
        _operationFacade = operationFacade ?? throw new ArgumentNullException(nameof(operationFacade));
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _categoryFacade = categoryFacade ?? throw new ArgumentNullException(nameof(categoryFacade));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public ImportResult Import(string data, DataFormat format)
    {
        var strategy = _strategyFactory.Resolve(format);
        var errors = new List<string>();
        var added = 0;

        foreach (var row in strategy.Parse(data))
        {
            if (!string.IsNullOrWhiteSpace(row.Error))
            {
                errors.Add(row.Error!);
                continue;
            }

            try
            {
                var command = ToCommand(row.Data!);
                _operationFacade.AddOperation(command);
                added++;
            }
            catch (Exception ex)
            {
                errors.Add($"Строка {row.LineNumber}: {ex.Message}");
            }
        }

        return ImportResult.From(added, errors);
    }

    private AddOperationCommand ToCommand(ImportRowData data)
    {
        if (!Guid.TryParse(data.AccountId, out var accountId))
        {
            throw new InvalidOperationException("Некорректный Id счета");
        }

        var account = _accounts.GetById(accountId) ?? throw new InvalidOperationException("Счет не найден для импорта");
        if (!Enum.TryParse<OperationType>(data.Type, true, out var type))
        {
            throw new InvalidOperationException("Некорректный тип операции");
        }

        var category = _categoryFacade.GetOrCreate(data.CategoryName, type);
        if (!Enum.TryParse<Currency>(data.Currency, true, out var currency))
        {
            currency = Currency.Rub;
        }

        if (!DateOnly.TryParse(data.Date, out var date))
        {
            date = DateOnly.FromDateTime(DateTime.Today);
        }

        return new AddOperationCommand(account.Id, category.Id, data.Amount, currency, type, date, data.Description);
    }
}
