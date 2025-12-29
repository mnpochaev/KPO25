using ZooErp.Domain.Abstractions;
using ZooErp.Domain.Zoo;

namespace ZooErp.Domain.Things;

public abstract class Thing : IInventory
{
    protected Thing(int inventoryNumber)
    {
        if (inventoryNumber <= 0)
        {
            throw new DomainException("Инвентарный номер должен быть положительным.");
        }

        InventoryNumber = inventoryNumber;
    }

    public int InventoryNumber { get; }

    public virtual string DisplayName => GetType().Name;
}
