namespace Gozon.Contracts.Messaging;

/// <summary>
/// Orders -> Payments
/// "Создан заказ, попробуй списать деньги"
/// </summary>
public sealed record OrderPaymentRequested(
    Guid MessageId,
    Guid OrderId,
    long UserId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc
);

/// <summary>
/// Payments -> Orders
/// "Оплата прошла успешно"
/// </summary>
public sealed record PaymentSucceeded(
    Guid MessageId,
    Guid OrderId,
    long UserId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc
);

/// <summary>
/// Payments -> Orders
/// "Оплата не прошла"
/// </summary>
public sealed record PaymentFailed(
    Guid MessageId,
    Guid OrderId,
    long UserId,
    decimal Amount,
    string Reason,
    DateTimeOffset OccurredAtUtc
);

/// <summary>
/// Orders -> Gateway
/// "Статус заказа поменялся"
/// </summary>
public sealed record OrderStatusChanged(
    Guid MessageId,
    Guid OrderId,
    long UserId,
    string Status,
    DateTimeOffset OccurredAtUtc
);
