namespace ZooErp.Domain.Animals;

public class Rabbit : Herbo
{
    public Rabbit(int foodKgPerDay, int inventoryNumber, int kindnessLevel)
        : base(foodKgPerDay, inventoryNumber, kindnessLevel)
    {
    }

    public override string DisplayName => "Кролик";
}
