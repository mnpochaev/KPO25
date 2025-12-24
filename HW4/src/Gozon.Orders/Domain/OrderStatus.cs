namespace Gozon.Orders.Domain;

/// <summary>
/// Статусы заказа
/// </summary>
public enum OrderStatus
{
    New = 0,
    Finished = 1,
    Cancelled = 2
}