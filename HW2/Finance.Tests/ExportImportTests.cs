using Finance.Application;
using Finance.Application.Accounts;
using Finance.Application.Commands;
using Finance.Application.Export;
using Finance.Application.Export.Visitors;
using Finance.Application.Import;
using Finance.Application.Metrics;
using Finance.Application.Notifications;
using Finance.Application.Operations;
using Finance.Application.Operations.Commands;
using Finance.Application.Operations.Filters;
using Finance.Application.Categories;
using Finance.Domain;
using Finance.Domain.Repositories;

namespace Finance.Tests;

public class ExportImportTests
{
    private readonly IBankAccountRepository _accounts = new InMemoryBankAccountRepository();
    private readonly ICategoryRepository _categories = new InMemoryCategoryRepository();
    private readonly IOperationRepository _operations = new InMemoryOperationRepository();

    private OperationFacade BuildFacade(IOperationObserver? observer = null)
    {
        var factory = new OperationFactory();
        var balance = new BalanceService(_accounts, _operations);
        var observers = observer is null ? null : new[] { observer };
        return new OperationFacade(_operations, _accounts, _categories, factory, balance, observers);
    }

    [Fact]
    public void Экспорт_CSV_содержит_строки()
    {
        var facade = BuildFacade();
        var metrics = new InMemoryScenarioMetrics();
        var visitors = new IExportVisitor[] { new CsvExportVisitor(), new JsonExportVisitor(), new YamlExportVisitor() };
        var exportService = new ExportService(_operations, new ExportVisitorFactory(visitors));
        var exportHandler = new TimedCommandHandler<ExportRequest, string>(new ExportCommandHandler(exportService), metrics);

        var account = new BankAccount("Основной", new Money(0));
        _accounts.Add(account);
        var salary = new Category("Зарплата", OperationType.Income);
        _categories.Add(salary);
        facade.AddOperation(new AddOperationCommand(account.Id, salary.Id, 1000, Currency.Rub, OperationType.Income, new DateOnly(2024, 1, 1), null));

        var csv = exportHandler.Handle(new ExportRequest(new OperationQuery(), DataFormat.Csv));
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 2);
    }

    [Fact]
    public void Импорт_JSON_создает_операции()
    {
        var facade = BuildFacade();
        var categoryFacade = new CategoryFacade(_categories);
        var importService = new ImportService(facade, new ImportStrategyFactory(new IImportStrategy[] { new JsonImportStrategy() }), categoryFacade, _accounts);
        var importHandler = new TimedCommandHandler<ImportRequest, ImportResult>(new ImportCommandHandler(importService), new InMemoryScenarioMetrics());

        var account = new BankAccount("Основной", new Money(0));
        _accounts.Add(account);

        var json = $@"
[
  {{""дата"": ""2024-02-01"", ""тип"": ""Income"", ""категория"": ""Кэшбэк"", ""сумма"": 150, ""валюта"": ""RUB"", ""счет"": ""{account.Id}""}}
]";

        Console.WriteLine(json);
        using (var doc = System.Text.Json.JsonDocument.Parse(json))
        {
            Assert.Equal(System.Text.Json.JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.True(doc.RootElement.GetArrayLength() > 0);
        }
        var parsed = new JsonImportStrategy().Parse(json).ToList();
        Assert.True(parsed.Count > 0);

        var result = importHandler.Handle(new ImportRequest(json, DataFormat.Json));

        Assert.True(result.Errors.Count == 0, string.Join(";", result.Errors));
        Assert.Equal(1, result.AddedCount);
        Assert.Single(_operations.GetAll());
    }
}
