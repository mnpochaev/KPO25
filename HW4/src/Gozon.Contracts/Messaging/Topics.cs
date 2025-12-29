namespace Gozon.Contracts.Messaging;

/// <summary>
/// Имена роутинг-ключей для событий.
/// </summary>
public static class Topics
{
    /// <summary>
    /// Orders -> Payments: "Создан заказ, нужно попытаться списать деньги".
    /// </summary>
    public const string OrderPaymentRequested = "order.payment.requested";

    /// <summary>
    /// Payments -> Orders: "Оплата прошла успешно".
    /// </summary>
    public const string PaymentSucceeded = "payment.succeeded";

    /// <summary>
    /// Payments -> Orders: "Оплата не прошла".
    /// </summary>
    public const string PaymentFailed = "payment.failed";

    /// <summary>
    /// Orders -> Gateway: "Статус заказа обновился".
    /// </summary>
    public const string OrderStatusChanged = "order.status.changed";
}
