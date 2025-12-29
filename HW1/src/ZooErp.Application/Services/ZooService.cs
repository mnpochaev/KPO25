using ZooErp.Application.Abstractions;
using ZooErp.Application.Models;
using ZooErp.Domain.Abstractions;
using ZooErp.Domain.Animals;
using ZooErp.Domain.Things;
using ZooErp.Domain.Zoo;

namespace ZooErp.Application.Services;

public class ZooService : IZooService
{
    private readonly IZooRepository _zooRepository;
    private readonly IVetClinic _vetClinic;

    public ZooService(IZooRepository zooRepository, IVetClinic vetClinic)
    {
        _zooRepository = zooRepository;
        _vetClinic = vetClinic;
    }

    public bool AcceptAnimal(AddAnimalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var zoo = _zooRepository.GetZoo();
        var animal = CreateAnimal(request);
        return zoo.AddAnimal(animal, _vetClinic);
    }

    public void RegisterThing(AddThingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var zoo = _zooRepository.GetZoo();
        var thing = CreateThing(request);
        zoo.AddThing(thing);
    }

    public ZooReportDto BuildReports()
    {
        var zoo = _zooRepository.GetZoo();

        var contactZooAnimals = zoo.GetContactZooAnimals()
            .Select(animal => animal.DisplayName)
            .ToList();

        var inventory = zoo.GetInventoryReport()
            .Select(item => new InventoryItemDto(item.InventoryNumber, item.Name))
            .ToList();

        return new ZooReportDto(
            zoo.GetAnimalsCount(),
            zoo.GetTotalFoodPerDay(),
            contactZooAnimals,
            inventory);
    }

    private static Animal CreateAnimal(AddAnimalRequest request) =>
        request.Kind switch
        {
            AnimalKind.Monkey => new Monkey(request.FoodKgPerDay, request.InventoryNumber, GetKindness(request)),
            AnimalKind.Rabbit => new Rabbit(request.FoodKgPerDay, request.InventoryNumber, GetKindness(request)),
            AnimalKind.Tiger => new Tiger(request.FoodKgPerDay, request.InventoryNumber),
            AnimalKind.Wolf => new Wolf(request.FoodKgPerDay, request.InventoryNumber),
            _ => throw new DomainException($"Неизвестный тип животного: {request.Kind}")
        };

    private static Thing CreateThing(AddThingRequest request) =>
        request.Kind switch
        {
            ThingKind.Table => new Table(request.InventoryNumber),
            ThingKind.Computer => new Computer(request.InventoryNumber),
            _ => throw new DomainException($"Неизвестный тип вещи: {request.Kind}")
        };

    private static int GetKindness(AddAnimalRequest request) =>
        request.KindnessLevel ?? throw new DomainException("Для травоядных обязателен уровень доброты.");
}
