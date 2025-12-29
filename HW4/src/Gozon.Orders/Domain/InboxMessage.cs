namespace Gozon.Orders.Domain;

public sealed class InboxMessage
{
    public Guid MessageId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}