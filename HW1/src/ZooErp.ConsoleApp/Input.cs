using ZooErp.Application.Models;

namespace ZooErp.ConsoleApp;

public static class Input
{
    public static int ReadInt(string prompt, int minInclusive = int.MinValue, int maxInclusive = int.MaxValue)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var line = Console.ReadLine();

            if (int.TryParse(line, out var value) && value >= minInclusive && value <= maxInclusive)
            {
                return value;
            }

            Console.WriteLine($"Введите число от {minInclusive} до {maxInclusive}.");
        }
    }

    public static AnimalKind ReadAnimalKind()
    {
        Console.WriteLine("Выберите животное:");
        Console.WriteLine("1 - Обезьяна");
        Console.WriteLine("2 - Кролик");
        Console.WriteLine("3 - Тигр");
        Console.WriteLine("4 - Волк");

        while (true)
        {
            var choice = ReadInt("Ваш выбор", 1, 4);
            return choice switch
            {
                1 => AnimalKind.Monkey,
                2 => AnimalKind.Rabbit,
                3 => AnimalKind.Tiger,
                4 => AnimalKind.Wolf,
                _ => throw new InvalidOperationException()
            };
        }
    }

    public static ThingKind ReadThingKind()
    {
        Console.WriteLine("Выберите вещь:");
        Console.WriteLine("1 - Стол");
        Console.WriteLine("2 - Компьютер");

        while (true)
        {
            var choice = ReadInt("Ваш выбор", 1, 2);
            return choice switch
            {
                1 => ThingKind.Table,
                2 => ThingKind.Computer,
                _ => throw new InvalidOperationException()
            };
        }
    }
}
