using System.Globalization;
using System.Linq;
using Finance.Application;
using Finance.Application.Accounts;
using Finance.Application.Analytics;
using Finance.Application.Budgets;
using Finance.Application.Categories;
using Finance.Application.Commands;
using Finance.Application.Decorators;
using Finance.Application.Export;
using Finance.Application.Export.Visitors;
using Finance.Application.Import;
using Finance.Application.Logging;
using Finance.Application.Metrics;
using Finance.Application.Notifications;
using Finance.Application.Operations;
using Finance.Application.Operations.Commands;
using Finance.Application.Operations.Filters;
using Finance.Domain;
using Finance.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

// Консольное приложение-демо. Все строки на русском.
var services = new ServiceCollection();

// Репозитории
services.AddSingleton<IBankAccountRepository, InMemoryBankAccountRepository>();
services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();
services.AddSingleton<IOperationRepository, InMemoryOperationRepository>();
services.AddSingleton<IBudgetRepository, InMemoryBudgetRepository>();

// Доменные сервисы/фабрики
services.AddSingleton<OperationFactory>();
services.AddSingleton<BalanceService>();
services.AddSingleton<AccountFacade>();
services.AddSingleton<CategoryFacade>();
services.AddSingleton<BudgetService>();
services.AddSingleton<INotificationSink, InMemoryNotificationSink>();
services.AddSingleton<IScenarioMetrics, InMemoryScenarioMetrics>();
services.AddSingleton<IOperationObserver>(sp => new BudgetExceededNotifier(sp.GetRequiredService<BudgetService>(), sp.GetRequiredService<INotificationSink>(), sp.GetRequiredService<ICategoryRepository>()));
services.AddSingleton<OperationFacade>();
services.AddSingleton<AnalyticsService>();

// Экспорт/импорт
services.AddSingleton<IExportVisitor, CsvExportVisitor>();
services.AddSingleton<IExportVisitor, JsonExportVisitor>();
services.AddSingleton<IExportVisitor, YamlExportVisitor>();
services.AddSingleton<ExportVisitorFactory>(sp => new ExportVisitorFactory(sp.GetServices<IExportVisitor>()));
services.AddSingleton<IExportService, ExportService>();
services.AddSingleton<IImportStrategy, CsvImportStrategy>();
services.AddSingleton<IImportStrategy, JsonImportStrategy>();
services.AddSingleton<IImportStrategy, YamlImportStrategy>();
services.AddSingleton<IImportStrategyFactory>(sp => new ImportStrategyFactory(sp.GetServices<IImportStrategy>()));
services.AddSingleton<ImportService>();

// Команды с декоратором тайминга
services.AddTransient<AddOperationCommandHandler>();
services.AddTransient<ICommandHandler<AddOperationCommand, Operation>>(sp => new TimedCommandHandler<AddOperationCommand, Operation>(sp.GetRequiredService<AddOperationCommandHandler>(), sp.GetRequiredService<IScenarioMetrics>()));
services.AddTransient<UpdateOperationCommandHandler>();
services.AddTransient<ICommandHandler<UpdateOperationCommand, Operation>>(sp => new TimedCommandHandler<UpdateOperationCommand, Operation>(sp.GetRequiredService<UpdateOperationCommandHandler>(), sp.GetRequiredService<IScenarioMetrics>()));
services.AddTransient<DeleteOperationCommandHandler>();
services.AddTransient<ICommandHandler<DeleteOperationCommand, bool>>(sp => new TimedCommandHandler<DeleteOperationCommand, bool>(sp.GetRequiredService<DeleteOperationCommandHandler>(), sp.GetRequiredService<IScenarioMetrics>()));
services.AddTransient<ExportCommandHandler>();
services.AddTransient<ICommandHandler<ExportRequest, string>>(sp => new TimedCommandHandler<ExportRequest, string>(sp.GetRequiredService<ExportCommandHandler>(), sp.GetRequiredService<IScenarioMetrics>()));
services.AddTransient<ImportCommandHandler>();
services.AddTransient<ICommandHandler<ImportRequest, ImportResult>>(sp => new TimedCommandHandler<ImportRequest, ImportResult>(sp.GetRequiredService<ImportCommandHandler>(), sp.GetRequiredService<IScenarioMetrics>()));

// Логирование экспорта (Decorator)
services.AddSingleton<ILogSink, InMemoryLogSink>();
services.AddSingleton<IExportService>(sp => new LoggingExportServiceDecorator(
    new ExportService(sp.GetRequiredService<IOperationRepository>(), sp.GetRequiredService<ExportVisitorFactory>()),
    sp.GetRequiredService<ILogSink>()));

var provider = services.BuildServiceProvider();
var accounts = provider.GetRequiredService<AccountFacade>();
var categories = provider.GetRequiredService<CategoryFacade>();
var analytics = provider.GetRequiredService<AnalyticsService>();
var budgetService = provider.GetRequiredService<BudgetService>();
var notificationSink = provider.GetRequiredService<INotificationSink>();
var metrics = provider.GetRequiredService<IScenarioMetrics>();
var exportHandler = provider.GetRequiredService<ICommandHandler<ExportRequest, string>>();
var importHandler = provider.GetRequiredService<ICommandHandler<ImportRequest, ImportResult>>();
var addOperation = provider.GetRequiredService<ICommandHandler<AddOperationCommand, Operation>>();
var updateOperation = provider.GetRequiredService<ICommandHandler<UpdateOperationCommand, Operation>>();
var deleteOperation = provider.GetRequiredService<ICommandHandler<DeleteOperationCommand, bool>>();
var operationFacade = provider.GetRequiredService<OperationFacade>();
var exportService = provider.GetRequiredService<IExportService>();
var importService = provider.GetRequiredService<ImportService>();
var logSink = provider.GetRequiredService<ILogSink>();

var running = true;
while (running)
{
    ShowMenu();
    var input = Console.ReadLine()?.Trim();
    switch (input)
    {
        case "1":
            HandleAccounts(accounts);
            break;
        case "2":
            HandleCategories(categories);
            break;
        case "3":
            HandleOperations(addOperation, updateOperation, deleteOperation, operationFacade, accounts, categories);
            break;
        case "4":
            ShowSummary(analytics);
            break;
        case "5":
            HandleBudget(budgetService, categories);
            break;
        case "6":
            HandleExport(exportHandler, operationFacade);
            break;
        case "7":
            HandleImport(importHandler);
            break;
        case "8":
            ShowNotifications(notificationSink);
            break;
        case "9":
            ShowMetrics(metrics);
            break;
        case "0":
            running = false;
            break;
        default:
            Console.WriteLine("Выберите пункт меню из списка.");
            break;
    }
}

static void ShowMenu()
{
    Console.WriteLine();
    Console.WriteLine("+------------------------------------------------+");
    Console.WriteLine("|               Финансы CLI (демо)               |");
    Console.WriteLine("+------------------------------------------------+");
    Console.WriteLine("| 1. Счета (создать/список/переименовать/удалить)|");
    Console.WriteLine("| 2. Категории (создать/список/переименовать/уд)|");
    Console.WriteLine("| 3. Операции (добавить/список/обновить/удалить)|");
    Console.WriteLine("| 4. Сводка (баланс доходы/расходы)             |");
    Console.WriteLine("| 5. Бюджет по категории                        |");
    Console.WriteLine("| 6. Экспорт CSV/JSON/YAML                      |");
    Console.WriteLine("| 7. Импорт CSV/JSON/YAML                       |");
    Console.WriteLine("| 8. Уведомления                                |");
    Console.WriteLine("| 9. Метрики сценариев                          |");
    Console.WriteLine("| 0. Выход                                      |");
    Console.WriteLine("+------------------------------------------------+");
    Console.Write("Ваш выбор: ");
}

static void HandleAccounts(AccountFacade accounts)
{
    var loop = true;
    while (loop)
    {
        Console.WriteLine("1 - Создать счет | 2 - Список | 3 - Переименовать | 4 - Удалить | 0 - Назад");
        Console.Write("Ваш выбор: ");
        var action = Console.ReadLine()?.Trim();
        switch (action)
        {
            case "1":
                Console.Write("Название счета: ");
                var name = Console.ReadLine() ?? string.Empty;
                var balance = ReadDecimal("Начальный баланс: ");
                if (balance is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                var account = accounts.Create(name, balance.Value);
                Console.WriteLine($"Создан счет {account.Id} ({account.Name}), баланс {account.Balance.Amount}");
                break;
            case "2":
                var list = accounts.List();
                if (!list.Any())
                {
                    Console.WriteLine("Счетов пока нет.");
                    break;
                }
                foreach (var acc in list)
                {
                    Console.WriteLine($"{acc.Id} | {acc.Name} | баланс {acc.Balance.Amount}");
                }
                break;
            case "3":
                var id = ReadGuid("Id счета: ");
                if (id is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                var existing = accounts.List().FirstOrDefault(a => a.Id == id);
                if (existing is null)
                {
                    Console.WriteLine("Счет не найден.");
                    break;
                }
                Console.Write("Новое имя: ");
                var newName = Console.ReadLine() ?? string.Empty;
                accounts.Rename(id.Value, newName);
                Console.WriteLine("Обновлено.");
                break;
            case "4":
                var delId = ReadGuid("Id счета: ");
                if (delId is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                if (accounts.List().All(a => a.Id != delId))
                {
                    Console.WriteLine("Счет не найден.");
                    break;
                }
                accounts.Delete(delId.Value);
                Console.WriteLine("Удалено.");
                break;
            case "0":
                loop = false;
                break;
            default:
                Console.WriteLine("Выберите действие из списка.");
                break;
        }
    }
}

static void HandleCategories(CategoryFacade categories)
{
    var loop = true;
    while (loop)
    {
        Console.WriteLine("1 - Создать категорию | 2 - Список | 3 - Переименовать | 4 - Удалить | 0 - Назад");
        Console.Write("Ваш выбор: ");
        var action = Console.ReadLine()?.Trim();
        switch (action)
        {
            case "1":
                Console.Write("Название: ");
                var name = Console.ReadLine() ?? string.Empty;
                var type = ReadOperationType();
                if (type is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                var category = categories.Create(name, type.Value);
                Console.WriteLine($"Создана категория {category.Id} {category.Name} ({FormatType(category.Kind)})");
                break;
            case "2":
                var list = categories.List();
                if (!list.Any())
                {
                    Console.WriteLine("Категорий пока нет.");
                    break;
                }
                foreach (var cat in list)
                {
                    Console.WriteLine($"{cat.Id} | {cat.Name} | {FormatType(cat.Kind)}");
                }
                break;
            case "3":
                var id = ReadGuid("Id категории: ");
                if (id is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                var existing = categories.List().FirstOrDefault(c => c.Id == id);
                if (existing is null)
                {
                    Console.WriteLine("Категория не найдена.");
                    break;
                }
                Console.Write("Новое имя: ");
                var newName = Console.ReadLine() ?? string.Empty;
                categories.Rename(id.Value, newName);
                Console.WriteLine("Обновлено.");
                break;
            case "4":
                var delId = ReadGuid("Id категории: ");
                if (delId is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                if (categories.List().All(c => c.Id != delId))
                {
                    Console.WriteLine("Категория не найдена.");
                    break;
                }
                categories.Delete(delId.Value);
                Console.WriteLine("Удалено.");
                break;
            case "0":
                loop = false;
                break;
            default:
                Console.WriteLine("Выберите действие из списка.");
                break;
        }
    }
}

static void HandleOperations(
    ICommandHandler<AddOperationCommand, Operation> add,
    ICommandHandler<UpdateOperationCommand, Operation> update,
    ICommandHandler<DeleteOperationCommand, bool> delete,
    OperationFacade facade,
    AccountFacade accounts,
    CategoryFacade categories)
{
    var loop = true;
    while (loop)
    {
        Console.WriteLine("1 - Добавить | 2 - Список | 3 - Обновить | 4 - Удалить | 0 - Назад");
        Console.Write("Ваш выбор: ");
        var action = Console.ReadLine()?.Trim();
        switch (action)
        {
            case "1":
                var addCmd = BuildAddOperationCommand(accounts, categories);
                if (addCmd is not null)
                {
                    var op = add.Handle(addCmd);
                    Console.WriteLine($"Операция {op.Id} добавлена на {op.Date:yyyy-MM-dd}");
                }
                break;
            case "2":
                var ops = facade.GetOperations(new OperationQuery());
                if (!ops.Any())
                {
                    Console.WriteLine("Операций пока нет.");
                    break;
                }
                foreach (var item in ops)
                {
                    Console.WriteLine($"{item.Id} | {item.Date:yyyy-MM-dd} | {item.BankAccountId} | {FormatType(item.Type)} | {item.Category.Name} | {item.Money.Amount} | {item.Description}");
                }
                break;
            case "3":
                var id = ReadGuid("Id операции: ");
                if (id is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                var upd = BuildUpdateOperationCommand(id.Value, accounts, categories);
                if (upd is not null)
                {
                    var updated = update.Handle(upd);
                    Console.WriteLine($"Операция {updated.Id} обновлена");
                }
                break;
            case "4":
                var delId = ReadGuid("Id операции: ");
                if (delId is null)
                {
                    Console.WriteLine("Отмена.");
                    break;
                }
                delete.Handle(new DeleteOperationCommand(delId.Value));
                Console.WriteLine("Удалено");
                break;
            case "0":
                loop = false;
                break;
            default:
                Console.WriteLine("Выберите действие из списка.");
                break;
        }
    }
}

static AddOperationCommand? BuildAddOperationCommand(AccountFacade accounts, CategoryFacade categories)
{
    var accountId = ReadGuid("Id счета: ");
    var categoryId = ReadGuid("Id категории: ");
    if (accountId is null || categoryId is null)
    {
        Console.WriteLine("Не выбраны счет или категория.");
        return null;
    }
    var type = ReadOperationType();
    if (type is null)
    {
        Console.WriteLine("Отмена.");
        return null;
    }
    var amount = ReadDecimal("Сумма: ");
    if (amount is null)
    {
        Console.WriteLine("Отмена.");
        return null;
    }
    var date = ReadDate("Дата (yyyy-MM-dd, пусто = сегодня): ", allowEmpty: true, defaultValue: DateOnly.FromDateTime(DateTime.Today));
    if (date is null && !string.IsNullOrWhiteSpace(date?.ToString()))
    {
        return null;
    }
    Console.Write("Описание: ");
    var description = Console.ReadLine();

    return new AddOperationCommand(accountId.Value, categoryId.Value, amount.Value, Currency.Rub, type.Value, date ?? DateOnly.FromDateTime(DateTime.Today), description);
}

static UpdateOperationCommand? BuildUpdateOperationCommand(Guid id, AccountFacade accounts, CategoryFacade categories)
{
    var accountId = ReadGuid("Id счета: ");
    var categoryId = ReadGuid("Id категории: ");
    if (accountId is null || categoryId is null)
    {
        Console.WriteLine("Не выбраны счет или категория.");
        return null;
    }
    var type = ReadOperationType();
    if (type is null)
    {
        Console.WriteLine("Отмена.");
        return null;
    }
    var amount = ReadDecimal("Сумма: ");
    if (amount is null)
    {
        Console.WriteLine("Отмена.");
        return null;
    }
    var date = ReadDate("Дата (yyyy-MM-dd): ", allowEmpty: false);
    if (date is null)
    {
        return null;
    }
    Console.Write("Описание: ");
    var description = Console.ReadLine();

    return new UpdateOperationCommand(id, accountId.Value, categoryId.Value, amount.Value, Currency.Rub, type.Value, date.Value, description);
}

static void ShowSummary(AnalyticsService analytics)
{
    var summary = analytics.GetSummary(new OperationQuery());
    Console.WriteLine($"Доходы: {summary.Income}, Расходы: {summary.Expense}, Баланс: {summary.Balance}");
}

static void HandleBudget(BudgetService budgetService, CategoryFacade categories)
{
    var catId = ReadGuid("Id категории (расход): ");
    if (catId is null)
    {
        Console.WriteLine("Отмена.");
        return;
    }
    var category = categories.List().FirstOrDefault(c => c.Id == catId);
    if (category is null)
    {
        Console.WriteLine("Категория не найдена");
        return;
    }

    var month = ReadYearMonth("Месяц (yyyy-MM): ");
    if (month is null)
    {
        Console.WriteLine("Отмена.");
        return;
    }
    var limit = ReadDecimal("Лимит: ");
    if (limit is null)
    {
        Console.WriteLine("Отмена.");
        return;
    }

    budgetService.SetMonthlyBudget(category, new Money(limit.Value, Currency.Rub), month);
    Console.WriteLine("Бюджет установлен.");
}

static Guid? ReadGuid(string prompt)
{
    Console.Write(prompt);
    var input = Console.ReadLine();
    if (Guid.TryParse(input, out var id))
    {
        return id;
    }

    Console.WriteLine("Некорректный Guid. Ожидается формат xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
    return null;
}

static OperationType? ReadOperationType()
{
    Console.Write("Тип операции (1 - Доход, 2 - Расход): ");
    var choice = Console.ReadLine()?.Trim();
    return choice switch
    {
        "1" => OperationType.Income,
        "2" => OperationType.Expense,
        _ => null
    };
}

static string FormatType(OperationType type) => type == OperationType.Income ? "Доход" : "Расход";

static decimal? ReadDecimal(string prompt)
{
    Console.Write(prompt);
    var input = Console.ReadLine();
    if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
    {
        return value;
    }

    Console.WriteLine("Некорректное число. Используйте формат 1234.56");
    return null;
}

static DateOnly? ReadDate(string prompt, bool allowEmpty, DateOnly? defaultValue = null)
{
    Console.Write(prompt);
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(input))
    {
        return allowEmpty ? defaultValue : null;
    }

    if (DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
        DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
    {
        return date;
    }

    Console.WriteLine("Некорректная дата. Ожидается yyyy-MM-dd.");
    return null;
}

static YearMonth? ReadYearMonth(string prompt)
{
    Console.Write(prompt);
    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(input))
    {
        return null;
    }

    if (DateTime.TryParseExact(input + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
    {
        return new YearMonth(dt.Year, dt.Month);
    }

    Console.WriteLine("Некорректный месяц. Формат yyyy-MM.");
    return null;
}

static void HandleExport(ICommandHandler<ExportRequest, string> exportHandler, OperationFacade facade)
{
    Console.Write("Формат (csv/json/yaml): ");
    var formatInput = Console.ReadLine()?.Trim().ToLowerInvariant();
    var format = formatInput switch
    {
        "json" => DataFormat.Json,
        "yaml" => DataFormat.Yaml,
        _ => DataFormat.Csv
    };

    Console.Write("Путь к файлу: ");
    var path = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(path))
    {
        Console.WriteLine("Путь не задан, экспорт отменен.");
        return;
    }

    var data = exportHandler.Handle(new ExportRequest(new OperationQuery(), format));
    File.WriteAllText(path, data);
    Console.WriteLine($"Экспортировано в {path}");
}

static void HandleImport(ICommandHandler<ImportRequest, ImportResult> importHandler)
{
    Console.Write("Формат (csv/json/yaml): ");
    var formatInput = Console.ReadLine()?.Trim().ToLowerInvariant();
    var format = formatInput switch
    {
        "json" => DataFormat.Json,
        "yaml" => DataFormat.Yaml,
        _ => DataFormat.Csv
    };

    Console.Write("Путь к файлу: ");
    var path = Console.ReadLine() ?? string.Empty;
    if (!File.Exists(path))
    {
        Console.WriteLine("Файл не найден.");
        return;
    }

    var text = File.ReadAllText(path);
    var result = importHandler.Handle(new ImportRequest(text, format));
    Console.WriteLine($"Импортировано: {result.AddedCount}, ошибок: {result.Errors.Count}");
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}

static void ShowNotifications(INotificationSink sink)
{
    if (!sink.Messages.Any())
    {
        Console.WriteLine("Уведомлений нет.");
        return;
    }

    Console.WriteLine("Уведомления:");
    foreach (var m in sink.Messages)
    {
        Console.WriteLine(" - " + m);
    }
}

static void ShowMetrics(IScenarioMetrics metrics)
{
    foreach (var entry in metrics.All)
    {
        var avg = entry.Value.Any() ? entry.Value.Average() : 0;
        Console.WriteLine($"{entry.Key}: вызовов {entry.Value.Count}, среднее {avg:F2} мс");
    }
}
