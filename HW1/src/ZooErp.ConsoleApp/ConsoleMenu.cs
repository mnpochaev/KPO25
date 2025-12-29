using ZooErp.Application.Abstractions;
using ZooErp.Application.Models;
using ZooErp.Domain.Zoo;

namespace ZooErp.ConsoleApp;

public class ConsoleMenu
{
    private readonly IZooService _zooService;

    public ConsoleMenu(IZooService zooService)
    {
        _zooService = zooService;
    }

    public void Run()
    {
        while (true)
        {
            PrintMenu();
            var choice = Input.ReadInt("Select an option", 0, 3);

            try
            {
                switch (choice)
                {
                    case 1:
                        HandleAddAnimal();
                        break;
                    case 2:
                        HandleAddThing();
                        break;
                    case 3:
                        HandleReport();
                        break;
                    case 0:
                        return;
                }
            }
            catch (DomainException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }
    }

    private void HandleAddAnimal()
    {
        var kind = Input.ReadAnimalKind();
        var food = Input.ReadInt("Корм в день (кг)", 1);
        var inventory = Input.ReadInt("Инвентарный номер", 1);
        int? kindness = kind is AnimalKind.Monkey or AnimalKind.Rabbit
            ? Input.ReadInt("Уровень доброты (0..10)", 0, 10)
            : null;

        var accepted = _zooService.AcceptAnimal(new AddAnimalRequest(kind, food, inventory, kindness));
        Console.WriteLine(accepted ? "Животное принято ветклиникой." : "Животное отклонено ветклиникой.");
    }

    private void HandleAddThing()
    {
        var kind = Input.ReadThingKind();
        var inventory = Input.ReadInt("Инвентарный номер", 1);

        _zooService.RegisterThing(new AddThingRequest(kind, inventory));
        Console.WriteLine("Вещь добавлена в инвентаризацию.");
    }

    private void HandleReport()
    {
        var report = _zooService.BuildReports();

        Console.WriteLine("=== Отчёт зоопарка ===");
        Console.WriteLine($"Количество животных: {report.AnimalsCount}");
        Console.WriteLine($"Общий корм в день: {report.TotalFoodPerDay} кг");

        Console.WriteLine("Животные контактного зоопарка:");
        if (report.ContactZooAnimals.Count == 0)
        {
            Console.WriteLine("  нет");
        }
        else
        {
            foreach (var name in report.ContactZooAnimals)
            {
                Console.WriteLine($"  • {name}");
            }
        }

        Console.WriteLine("Инвентарь:");
        if (report.InventoryItems.Count == 0)
        {
            Console.WriteLine("  пусто");
        }
        else
        {
            foreach (var item in report.InventoryItems)
            {
                Console.WriteLine($"  • №{item.InventoryNumber}: {item.Name}");
            }
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("1 - Добавить животное");
        Console.WriteLine("2 - Добавить вещь");
        Console.WriteLine("3 - Показать отчёт");
        Console.WriteLine("0 - Выход");
    }
}
