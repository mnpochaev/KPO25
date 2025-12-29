namespace ZooErp.Application.Models;

public enum ThingKind
{
    Table,
    Computer
}

public sealed record AddThingRequest(
    ThingKind Kind,
    int InventoryNumber);
