using System;
using System.Collections.Generic;
using System.Linq;
using Finance.Application.Operations.Filters;
using Finance.Domain;
using Finance.Domain.Repositories;
using Finance.Domain.Specifications;

namespace Finance.Application.Analytics;

/// <summary>
/// Сервис аналитики операций.
/// </summary>
public sealed class AnalyticsService
{
    private readonly IOperationRepository _repository;

    public AnalyticsService(IOperationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public AnalyticsSummary GetSummary(OperationQuery? query)
    {
        var transactions = FilterOperations(query);
        var income = transactions.Where(t => t.Type == OperationType.Income).Sum(t => t.Money.Amount);
        var expense = transactions.Where(t => t.Type == OperationType.Expense).Sum(t => t.Money.Amount);
        return new AnalyticsSummary(income, expense);
    }

    public IReadOnlyDictionary<string, decimal> GetExpenseByCategory(OperationQuery? query)
    {
        var builder = new OperationFilterBuilder();
        builder.WithCategory(query?.CategoryId)
            .WithAccount(query?.AccountId)
            .WithinDateRange(query?.DateRange);

        var spec = builder.Build().And(new OperationTypeSpecification(OperationType.Expense));
        var expenses = _repository.Query(spec);

        return expenses
            .GroupBy(t => t.Category.Name)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Money.Amount));
    }

    private IReadOnlyCollection<Operation> FilterOperations(OperationQuery? query)
    {
        var builder = new OperationFilterBuilder();
        builder.WithType(query?.Type)
            .WithCategory(query?.CategoryId)
            .WithAccount(query?.AccountId)
            .WithinDateRange(query?.DateRange);

        var spec = builder.Build();
        return _repository.Query(spec).ToList();
    }
}
