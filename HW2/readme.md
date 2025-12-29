# Домашняя работа №2

## Что реализовано
- Доменные сущности и объекты-значения: `BankAccount`, `Operation` (доход/расход), `Category`, `Budget`, `Money`, `DateRange`, `YearMonth`, `OperationDescription`.
- Фасады и сервисы: работа со счетами/категориями/операциями, аналитика (баланс/распределения), бюджеты с уведомлениями, пересчёт балансов.
- Экспорт/импорт: CSV/JSON/YAML (Visitor + Template Method), пропуск некорректных строк с отчётом об ошибках.
- Уведомления о превышении бюджета (Observer) и метрики сценариев (Command + Decorator с таймингом).
- Консольный CLI на русском с проверками ввода и DI-контейнером (`Microsoft.Extensions.DependencyInjection`).
- Хранение в памяти: InMemory-репозитории для счетов, категорий, операций, бюджетов; Proxy-кэш категорий.

## Использованные паттерны GoF
- **Factory Method** — `OperationFactory` создаёт операции с валидацией.
- **Facade** — `AccountFacade`, `CategoryFacade`, `OperationFacade` скрывают доменную работу за простыми методами.
- **Command + Decorator** — `ICommandHandler` + `TimedCommandHandler` измеряют время сценариев (добавление/обновление/удаление операции, импорт, экспорт).
- **Builder + Specification** — `OperationFilterBuilder` собирает спецификации (`OperationTypeSpecification`, `CategorySpecification`, `AccountSpecification`, `DateRangeSpecification`, `AndSpecification`, `TrueSpecification`).
- **Observer** — `IOperationObserver` + `BudgetExceededNotifier` уведомляют о превышении бюджета.
- **Template Method** — `ImportStrategyBase` задаёт общий процесс импорта для CSV/JSON/YAML.
- **Visitor** — `CsvExportVisitor`, `JsonExportVisitor`, `YamlExportVisitor` формируют экспорт в разные форматы.
- **Proxy** — `CachingCategoryRepositoryProxy` кэширует категории поверх базового репозитория.
- **Decorator (логирование)** — `LoggingExportServiceDecorator` логирует экспорт.

## Структура решений
- `Finance.Domain` — доменные модели, спецификации, репозитории в памяти, прокси.
- `Finance.Application` — фасады, сервисы, команды, декораторы, экспорт/импорт, аналитика, бюджеты, метрики.
- `Finance.Tests` — 11 юнит-тестов (валидации, фильтры, аналитика, бюджеты, импорт/экспорт).
- `Finance.Cli` — консольный интерфейс на русском.

## Запуск
- Сборка: `dotnet build`
- Тесты: `dotnet test` (11/11 зелёные)
- CLI: `dotnet run --project Finance.Cli/Finance.Cli.csproj`

