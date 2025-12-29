using System;
using System.Linq;
using Finance.Application.Operations.Filters;
using Finance.Domain.Repositories;

namespace Finance.Application.Export;

/// <summary>
/// Сервис экспорта транзакций.
/// </summary>
public sealed class ExportService : IExportService
{
    private readonly IOperationRepository _repository;
    private readonly ExportVisitorFactory _visitorFactory;

    public ExportService(IOperationRepository repository, ExportVisitorFactory visitorFactory)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _visitorFactory = visitorFactory ?? throw new ArgumentNullException(nameof(visitorFactory));
    }

    public string Export(OperationQuery? query, DataFormat format)
    {
        var builder = new OperationFilterBuilder();
        builder.WithType(query?.Type)
            .WithCategory(query?.CategoryId)
            .WithAccount(query?.AccountId)
            .WithinDateRange(query?.DateRange);

        var spec = builder.Build();
        var operations = _repository.Query(spec)
            .OrderByDescending(t => t.Date)
            .ToList();

        var visitor = _visitorFactory.Resolve(format);
        var context = new ExportContext(operations);
        return context.Accept(visitor);
    }
}
