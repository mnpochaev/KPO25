using ZooErp.Domain.Abstractions;
using ZooErp.Domain.Animals;
using ZooErp.Domain.Things;

namespace ZooErp.Domain.Zoo;

public class Zoo
{
    private readonly List<Animal> _animals = new();
    private readonly List<Thing> _things = new();

    public IReadOnlyCollection<Animal> Animals => _animals.AsReadOnly();

    public IReadOnlyCollection<Thing> Things => _things.AsReadOnly();

    public bool AddAnimal(Animal animal, IVetClinic vetClinic)
    {
        ArgumentNullException.ThrowIfNull(animal);
        ArgumentNullException.ThrowIfNull(vetClinic);

        EnsureInventoryNumberIsFree(animal.InventoryNumber);

        if (!vetClinic.IsHealthy(animal))
        {
            return false;
        }

        _animals.Add(animal);
        return true;
    }

    public void AddThing(Thing thing)
    {
        ArgumentNullException.ThrowIfNull(thing);
        EnsureInventoryNumberIsFree(thing.InventoryNumber);
        _things.Add(thing);
    }

    public int GetAnimalsCount() => _animals.Count;

    public int GetTotalFoodPerDay() => _animals.Sum(animal => animal.FoodKgPerDay);

    public IReadOnlyCollection<Herbo> GetContactZooAnimals() =>
        _animals.OfType<Herbo>().Where(animal => animal.CanBeInContactZoo).ToList();

    public IReadOnlyCollection<(int InventoryNumber, string Name)> GetInventoryReport()
    {
        var animals = _animals.Select(animal => (animal.InventoryNumber, animal.DisplayName));
        var things = _things.Select(thing => (thing.InventoryNumber, thing.DisplayName));

        return animals.Concat(things)
            .OrderBy(item => item.InventoryNumber)
            .ToList();
    }

    private void EnsureInventoryNumberIsFree(int inventoryNumber)
    {
        var existsInAnimals = _animals.Any(animal => animal.InventoryNumber == inventoryNumber);
        var existsInThings = _things.Any(thing => thing.InventoryNumber == inventoryNumber);

        if (existsInAnimals || existsInThings)
        {
            throw new DomainException($"Инвентарный номер {inventoryNumber} уже используется.");
        }
    }
}
