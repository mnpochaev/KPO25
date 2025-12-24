namespace Gozon.Orders.Domain;

/// <summary>
/// Заказ пользователя
/// </summary>
public sealed class Order
{
    public Guid Id { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}