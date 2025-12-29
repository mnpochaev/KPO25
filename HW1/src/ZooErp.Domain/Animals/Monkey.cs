namespace ZooErp.Domain.Animals;

public class Monkey : Herbo
{
    public Monkey(int foodKgPerDay, int inventoryNumber, int kindnessLevel)
        : base(foodKgPerDay, inventoryNumber, kindnessLevel)
    {
    }

    public override string DisplayName => "Обезьяна";
}
