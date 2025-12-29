namespace ZooErp.Domain.Animals;

public class Tiger : Predator
{
    public Tiger(int foodKgPerDay, int inventoryNumber)
        : base(foodKgPerDay, inventoryNumber)
    {
    }

    public override string DisplayName => "Тигр";
}
