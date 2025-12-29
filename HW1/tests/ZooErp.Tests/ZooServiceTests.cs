using ZooErp.Application.Models;
using ZooErp.Application.Services;
using ZooErp.Domain.Abstractions;
using ZooErp.Domain.Animals;
using ZooErp.Infrastructure.Storage;

namespace ZooErp.Tests;

public class ZooServiceTests
{
    [Fact]
    public void AcceptAnimalAddsToInventoryWhenHealthy()
    {
        // Arrange
        var repository = new InMemoryZooStore();
        var vet = new StubVetClinic(true);
        var service = new ZooService(repository, vet);

        // Act
        var accepted = service.AcceptAnimal(new AddAnimalRequest(AnimalKind.Tiger, 6, 1, null));

        // Assert
        Assert.True(accepted);
        Assert.Equal(1, repository.GetZoo().GetAnimalsCount());
    }

    [Fact]
    public void AcceptAnimalDoesNotAddWhenVetRejects()
    {
        // Arrange
        var repository = new InMemoryZooStore();
        var vet = new StubVetClinic(false);
        var service = new ZooService(repository, vet);

        // Act
        var accepted = service.AcceptAnimal(new AddAnimalRequest(AnimalKind.Rabbit, 3, 1, 6));

        // Assert
        Assert.False(accepted);
        Assert.Equal(0, repository.GetZoo().GetAnimalsCount());
    }

    [Fact]
    public void BuildReportsAggregatesZooData()
    {
        // Arrange
        var repository = new InMemoryZooStore();
        var vet = new StubVetClinic(true);
        var service = new ZooService(repository, vet);

        service.AcceptAnimal(new AddAnimalRequest(AnimalKind.Monkey, 2, 1, 9));
        service.AcceptAnimal(new AddAnimalRequest(AnimalKind.Wolf, 5, 2, null));
        service.RegisterThing(new AddThingRequest(ThingKind.Table, 3));

        // Act
        var report = service.BuildReports();

        // Assert
        Assert.Equal(2, report.AnimalsCount);
        Assert.Equal(7, report.TotalFoodPerDay);
        Assert.Contains("Обезьяна", report.ContactZooAnimals);
        Assert.DoesNotContain("Волк", report.ContactZooAnimals);
        Assert.Equal(3, report.InventoryItems.Count);
        Assert.Contains(report.InventoryItems, item => item.InventoryNumber == 3 && item.Name == "Стол");
    }

    private sealed class StubVetClinic : IVetClinic
    {
        private readonly bool _isHealthy;

        public StubVetClinic(bool isHealthy)
        {
            _isHealthy = isHealthy;
        }

        public bool IsHealthy(Animal animal)
        {
            ArgumentNullException.ThrowIfNull(animal);
            return _isHealthy;
        }
    }
}
