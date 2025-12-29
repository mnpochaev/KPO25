using ZooErp.Domain.Zoo;

namespace ZooErp.Domain.Animals;

public abstract class Herbo : Animal
{
    protected Herbo(int foodKgPerDay, int inventoryNumber, int kindnessLevel)
        : base(foodKgPerDay, inventoryNumber)
    {
        if (kindnessLevel is < 0 or > 10)
        {
            throw new DomainException("Уровень доброты должен быть от 0 до 10.");
        }

        KindnessLevel = kindnessLevel;
    }

    public int KindnessLevel { get; }

    public bool CanBeInContactZoo => KindnessLevel > 5;
}
