namespace Gozon.Payments.Domain;

/// <summary>
/// Inbox для обработки входящих сообщений
/// </summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}