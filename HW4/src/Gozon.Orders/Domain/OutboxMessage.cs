namespace Gozon.Orders.Domain;

/// <summary>
/// Сообщение Outbox:
/// сохраняется в БД вместе с заказом в одной транзакции,
/// а публикуется в брокере отдельным фоновым воркером.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>Тип события.</summary>
    public string Type { get; set; } = null!;

    /// <summary>Тело события в JSON.</summary>
    public string PayloadJson { get; set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>Когда реально отправили в брокер. Пока null — ещё не опубликовано.</summary>
    public DateTimeOffset? PublishedAtUtc { get; set; }

    /// <summary>Сколько раз пытались опубликовать.</summary>
    public int Attempts { get; set; }

    /// <summary>Последняя ошибка публикации (если была).</summary>
    public string? LastError { get; set; }
}