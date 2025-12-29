using ZooErp.Domain.Abstractions;
using ZooErp.Domain.Animals;
using ZooErp.Domain.Things;
using ZooErp.Domain.Zoo;

namespace ZooErp.Tests;

public class ZooTests
{
    [Fact]
    public void UnhealthyAnimalIsNotAccepted()
    {
        // Arrange
        var zoo = new Zoo();
        var vet = new StubVetClinic(false);
        var tiger = new Tiger(10, 1);

        // Act
        var accepted = zoo.AddAnimal(tiger, vet);

        // Assert
        Assert.False(accepted);
        Assert.Empty(zoo.Animals);
    }

    [Fact]
    public void HealthyAnimalIsAcceptedAndAddedToInventory()
    {
        // Arrange
        var zoo = new Zoo();
        var vet = new StubVetClinic(true);
        var rabbit = new Rabbit(3, 2, 8);

        // Act
        var accepted = zoo.AddAnimal(rabbit, vet);

        // Assert
        Assert.True(accepted);
        Assert.Equal(1, zoo.GetAnimalsCount());
        var report = zoo.GetInventoryReport();
        Assert.Contains(report, item => item.InventoryNumber == 2 && item.Name == "Кролик");
    }

    [Fact]
    public void TotalFoodIsCalculatedCorrectly()
    {
        // Arrange
        var zoo = new Zoo();
        var vet = new StubVetClinic(true);
        zoo.AddAnimal(new Rabbit(2, 1, 6), vet);
        zoo.AddAnimal(new Tiger(5, 2), vet);

        // Act
        var totalFood = zoo.GetTotalFoodPerDay();

        // Assert
        Assert.Equal(7, totalFood);
    }

    [Fact]
    public void ContactZooContainsOnlyKindHerbivores()
    {
        // Arrange
        var zoo = new Zoo();
        var vet = new StubVetClinic(true);
        zoo.AddAnimal(new Rabbit(2, 1, 4), vet);
        zoo.AddAnimal(new Monkey(3, 2, 8), vet);
        zoo.AddAnimal(new Wolf(6, 3), vet);

        // Act
        var contactAnimals = zoo.GetContactZooAnimals();

        // Assert
        Assert.Single(contactAnimals);
        Assert.Equal("Обезьяна", contactAnimals.First().DisplayName);
    }

    [Fact]
    public void InventoryReportContainsAnimalsAndThings()
    {
        // Arrange
        var zoo = new Zoo();
        var vet = new StubVetClinic(true);
        zoo.AddAnimal(new Tiger(4, 3), vet);
        zoo.AddThing(new Table(1));
        zoo.AddThing(new Computer(2));

        // Act
        var report = zoo.GetInventoryReport();

        // Assert
        Assert.Equal(3, report.Count);
        Assert.Contains(report, item => item.InventoryNumber == 1 && item.Name == "Стол");
        Assert.Contains(report, item => item.InventoryNumber == 2 && item.Name == "Компьютер");
        Assert.Contains(report, item => item.InventoryNumber == 3 && item.Name == "Тигр");
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
