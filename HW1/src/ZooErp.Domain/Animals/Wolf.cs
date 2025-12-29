namespace ZooErp.Domain.Animals;

public class Wolf : Predator
{
    public Wolf(int foodKgPerDay, int inventoryNumber)
        : base(foodKgPerDay, inventoryNumber)
    {
    }

    public override string DisplayName => "Волк";
}
