# Zoo ERP


## Как запустить
- Собрать: `dotnet build -m:1`
- Консоль: `dotnet run --project src/ZooErp.ConsoleApp/ZooErp.ConsoleApp.csproj`
- Тесты: `dotnet test -m:1`

## Где что:
- `src/ZooErp.Domain` — сущности животных, вещей, агрегат `Zoo`, интерфейсы `IAlive`, `IInventory`, ветклиника.
- `src/ZooErp.Application` — сервис `IZooService`, запросы/DTO
- `src/ZooErp.Infrastructure` — ветклиника и in-memory хранилище зоопарка.
- `src/ZooErp.ConsoleApp` — меню и ввод/вывод в консоль, зависимости берутся через DI.
- `tests/ZooErp.Tests` — xUnit-тесты для домена и сервиса.

## SOLID
- S: доменные классы держат только правила и данные, вывод и хранение вынесены.
- O: новое животное — новый класс, существующий код трогать не нужно.
- L: животные корректно работают там, где ожидают `IAlive`/`IInventory`.
- I: `IAlive` и `IInventory` разделены для гибкости (например, люди — только живые).
- D: зависимости проходят через интерфейсы (`IVetClinic`, `IZooService`, `IZooRepository`), реализации в инфраструктуре/консоли.

## DI в приложении
`Program.cs` регистрирует `IZooRepository -> InMemoryZooStore`, `IVetClinic -> SimpleVetClinic`, `IZooService -> ZooService`, меню как singleton. Консоль получает всё из контейнера.

## Тесты и покрытие
Тесты проверяют приёмку здорового/нездорового животного, расчёт корма, контактный зоопарк и инвентаризацию. Покрытие можно собрать: `dotnet test /p:CollectCoverage=true`.
