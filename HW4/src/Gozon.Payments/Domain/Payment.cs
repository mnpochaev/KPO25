namespace Gozon.Payments.Domain;

/// <summary>
/// Платёж по заказу. Делаем OrderId уникальным, чтобы не списать дважды за один заказ.
/// </summary>
public sealed class Payment
{
    public Guid OrderId { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>
    /// Условный статус платежа
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }
}