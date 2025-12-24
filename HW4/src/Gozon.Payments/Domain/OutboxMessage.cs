namespace Gozon.Payments.Domain;

/// <summary>
/// Outbox для публикации исходящих событий
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}