namespace ZooErp.Application.Models;

public sealed record ZooReportDto(
    int AnimalsCount,
    int TotalFoodPerDay,
    IReadOnlyCollection<string> ContactZooAnimals,
    IReadOnlyCollection<InventoryItemDto> InventoryItems);
