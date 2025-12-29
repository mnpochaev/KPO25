using System.Text;
using System.Text.Json;
using Gozon.Contracts.Messaging;
using Gozon.Orders.Data;
using Gozon.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gozon.Orders.Messaging;

/// <summary>
/// Консьюмер событий оплаты: читает успех/фейл и обновляет статус заказа.
/// </summary>
public sealed class OrdersPaymentsEventsConsumer : BackgroundService
{
    private const string QueueName = "gozon.orders";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _rmq;
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<OrdersPaymentsEventsConsumer> _logger;

    public OrdersPaymentsEventsConsumer(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory rmq,
        IOptions<RabbitMqOptions> opt,
        ILogger<OrdersPaymentsEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _rmq = rmq;
        _opt = opt.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = _rmq.GetConnection().CreateModel();

        channel.ExchangeDeclare(_opt.Exchange, type: "topic", durable: true, autoDelete: false);

        var dlxExchange = $"{_opt.Exchange}.dlx";
        var dlqName = $"{QueueName}.dlq";

        channel.ExchangeDeclare(dlxExchange, type: "topic", durable: true, autoDelete: false);

        channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlxExchange,
            ["x-dead-letter-routing-key"] = dlqName
        });
        channel.QueueBind(QueueName, _opt.Exchange, Topics.PaymentSucceeded);
        channel.QueueBind(QueueName, _opt.Exchange, Topics.PaymentFailed);

        channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(dlqName, dlxExchange, dlqName);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            const int maxAttempts = 5;
            var attempt = 0;

            while (!stoppingToken.IsCancellationRequested && attempt < maxAttempts)
            {
                try
                {
                    await HandleMessage(ea, stoppingToken);
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
                    _logger.LogError(ex, "Не удалось обработать событие оплаты (попытка {Attempt}/{Max})", attempt, maxAttempts);

                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError("Сообщение {MessageId} отправлено в DLQ после {Attempts} попыток", ea.BasicProperties?.MessageId, attempt);
                        channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    await Task.Delay(delay, stoppingToken);
                }
            }
        };

        channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        stoppingToken.Register(() =>
        {
            try { channel.Close(); } catch { /* ignore */ }
            channel.Dispose();
        });

        return Task.CompletedTask;
    }

    private async Task HandleMessage(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var type = ea.BasicProperties?.Type;
        var msgIdStr = ea.BasicProperties?.MessageId;

        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(msgIdStr) || !Guid.TryParse(msgIdStr, out var msgId))
            return; // некорректное сообщение — просто подтверждаем

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var already = await db.InboxMessages.AnyAsync(x => x.MessageId == msgId, cancellationToken);
        if (already)
        {
            await tx.CommitAsync(cancellationToken);
            return;
        }

        var json = Encoding.UTF8.GetString(ea.Body.ToArray());

        string? newStatus = null;
        Guid? statusOrderId = null;
        long? statusUserId = null;

        if (type == nameof(PaymentSucceeded))
        {
            var ev = JsonSerializer.Deserialize<PaymentSucceeded>(json)!;

            var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == ev.OrderId, cancellationToken);
            if (order != null)
            {
                order.Status = OrderStatus.Finished;
                newStatus = OrderStatus.Finished.ToString();
                statusOrderId = ev.OrderId;
                statusUserId = ev.UserId;
            }
        }
        else if (type == nameof(PaymentFailed))
        {
            var ev = JsonSerializer.Deserialize<PaymentFailed>(json)!;

            var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == ev.OrderId, cancellationToken);
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled;
                newStatus = OrderStatus.Cancelled.ToString();
                statusOrderId = ev.OrderId;
                statusUserId = ev.UserId;
            }
        }

        db.InboxMessages.Add(new InboxMessage
        {
            MessageId = msgId,
            ReceivedAtUtc = DateTimeOffset.UtcNow
        });

        if (newStatus is not null && statusOrderId is not null && statusUserId is not null)
        {
            var statusEvent = new OrderStatusChanged(
                MessageId: Guid.NewGuid(),
                OrderId: statusOrderId.Value,
                UserId: statusUserId.Value,
                Status: newStatus,
                OccurredAtUtc: DateTimeOffset.UtcNow
            );

            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(OrderStatusChanged),
                PayloadJson = JsonSerializer.Serialize(statusEvent),
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Attempts = 0
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
