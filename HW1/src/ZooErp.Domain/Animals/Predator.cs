namespace ZooErp.Domain.Animals;

public abstract class Predator : Animal
{
    protected Predator(int foodKgPerDay, int inventoryNumber)
        : base(foodKgPerDay, inventoryNumber)
    {
    }
}
