using ZooErp.Domain.Abstractions;
using ZooErp.Domain.Zoo;

namespace ZooErp.Domain.Animals;

public abstract class Animal : IAlive, IInventory
{
    protected Animal(int foodKgPerDay, int inventoryNumber)
    {
        if (foodKgPerDay <= 0)
        {
            throw new DomainException("Ежедневный корм должен быть положительным.");
        }

        if (inventoryNumber <= 0)
        {
            throw new DomainException("Инвентарный номер должен быть положительным.");
        }

        FoodKgPerDay = foodKgPerDay;
        InventoryNumber = inventoryNumber;
    }

    public int FoodKgPerDay { get; }

    public int InventoryNumber { get; }

    public virtual string DisplayName => GetType().Name;
}
