namespace Gozon.Contracts.Messaging;

public static class TopicResolver
{
    public static string ForMessageType(string type) => type switch
    {
        nameof(OrderPaymentRequested) => Topics.OrderPaymentRequested,
        nameof(PaymentSucceeded)      => Topics.PaymentSucceeded,
        nameof(PaymentFailed)         => Topics.PaymentFailed,
        nameof(OrderStatusChanged)    => Topics.OrderStatusChanged,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown message type")
    };
}
