namespace ZooErp.Application.Models;

public enum AnimalKind
{
    Monkey,
    Rabbit,
    Tiger,
    Wolf
}

public sealed record AddAnimalRequest(
    AnimalKind Kind,
    int FoodKgPerDay,
    int InventoryNumber,
    int? KindnessLevel);
