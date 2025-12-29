namespace ZooErp.Domain.Things;

public class Table : Thing
{
    public Table(int inventoryNumber)
        : base(inventoryNumber)
    {
    }

    public override string DisplayName => "Стол";
}
