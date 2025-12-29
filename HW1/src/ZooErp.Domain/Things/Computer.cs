namespace ZooErp.Domain.Things;

public class Computer : Thing
{
    public Computer(int inventoryNumber)
        : base(inventoryNumber)
    {
    }

    public override string DisplayName => "Компьютер";
}
