using System.Text;
using System.Text.Json;
using Gozon.Contracts.Messaging;
using Gozon.Payments.Data;
using Gozon.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gozon.Payments.Messaging;

/// <summary>
/// Консьюмер заказов: списывает деньги и кладёт событие в outbox.
/// </summary>
public sealed class PaymentsOrdersConsumer : BackgroundService
{
    private const string QueueName = "gozon.payments";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _rmq;
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<PaymentsOrdersConsumer> _logger;

    public PaymentsOrdersConsumer(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory rmq,
        IOptions<RabbitMqOptions> opt,
        ILogger<PaymentsOrdersConsumer> logger)
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
        channel.QueueBind(QueueName, _opt.Exchange, Topics.OrderPaymentRequested);

        channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(dlqName, dlxExchange, dlqName);

        channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            const int maxAttempts = 3;
            var attempt = 0;
            OrderPaymentRequested? req = null;
            Guid msgId = default;

            while (!stoppingToken.IsCancellationRequested && attempt < maxAttempts)
            {
                try
                {
                    (req, msgId) = await HandleMessage(ea, stoppingToken);
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
                    _logger.LogError(ex, "Не удалось обработать OrderPaymentRequested (попытка {Attempt}/{Max})", attempt, maxAttempts);

                    if (attempt >= maxAttempts)
                    {
                        try
                        {
                            if (req is null)
                            {
                                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                                req = JsonSerializer.Deserialize<OrderPaymentRequested>(json);
                            }

                            await MarkInfrastructureFailure(req, ea, attempt, stoppingToken);
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception finalEx)
                        {
                            _logger.LogError(finalEx, "Не удалось зафиксировать неуспешную оплату после {Attempts} попыток", attempt);
                            channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                        }

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

    private async Task<(OrderPaymentRequested req, Guid msgId)> HandleMessage(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var type = ea.BasicProperties?.Type;
        var msgIdStr = ea.BasicProperties?.MessageId;

        // Нам важны Type и MessageId, иначе не сможем сделать дедуп
        if (type != nameof(OrderPaymentRequested) ||
            string.IsNullOrWhiteSpace(msgIdStr) ||
            !Guid.TryParse(msgIdStr, out var msgId))
            throw new InvalidOperationException("Некорректное сообщение OrderPaymentRequested");

        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var req = JsonSerializer.Deserialize<OrderPaymentRequested>(json);

        if (req is null)
            throw new InvalidOperationException("Не удалось десериализовать OrderPaymentRequested");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        // 1) Transactional Inbox (дедуп по MessageId)
        var already = await db.InboxMessages.AnyAsync(x => x.MessageId == msgId, cancellationToken);
        if (already)
        {
            await tx.CommitAsync(cancellationToken);
            return (req, msgId);
        }

        // 2) Доп. защита: не создаём повторный Payment для одного OrderId
        var paymentExists = await db.Payments.AnyAsync(x => x.OrderId == req.OrderId, cancellationToken);
        if (paymentExists)
        {
            db.InboxMessages.Add(new InboxMessage
            {
                MessageId = msgId,
                ProcessedAtUtc = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return (req, msgId);
        }

        var now = DateTimeOffset.UtcNow;

        // 3) Проверяем наличие счёта, если нет — сразу Fail с причиной.
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.UserId == req.UserId, cancellationToken);
        if (account is null)
        {
            db.Payments.Add(new Payment
            {
                OrderId = req.OrderId,
                UserId = req.UserId,
                Amount = req.Amount,
                Status = "Failed",
                CreatedAtUtc = now
            });

            var ev = new PaymentFailed(
                MessageId: Guid.NewGuid(),
                OrderId: req.OrderId,
                UserId: req.UserId,
                Amount: req.Amount,
                Reason: "Счёт не найден",
                OccurredAtUtc: now
            );

            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(PaymentFailed),
                PayloadJson = JsonSerializer.Serialize(ev),
                OccurredAtUtc = now,
                Attempts = 0
            });
        }
        else
        {
            // 4) Пытаемся списать деньги атомарно (CAS)
            var rows = await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE accounts SET \"Balance\" = \"Balance\" - {req.Amount} WHERE \"UserId\" = {req.UserId} AND \"Balance\" >= {req.Amount}",
                cancellationToken);

            if (rows == 1)
            {
                // 4a) Успех: пишем Payment + Outbox(PaymentSucceeded)
                db.Payments.Add(new Payment
                {
                    OrderId = req.OrderId,
                    UserId = req.UserId,
                    Amount = req.Amount,
                    Status = "Succeeded",
                    CreatedAtUtc = now
                });

                var ev = new PaymentSucceeded(
                    MessageId: Guid.NewGuid(),
                    OrderId: req.OrderId,
                    UserId: req.UserId,
                    Amount: req.Amount,
                    OccurredAtUtc: now
                );

                db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(PaymentSucceeded),
                    PayloadJson = JsonSerializer.Serialize(ev),
                    OccurredAtUtc = now,
                    Attempts = 0
                });
            }
            else
            {
                // 4b) Денег не хватило: пишем Fail
                db.Payments.Add(new Payment
                {
                    OrderId = req.OrderId,
                    UserId = req.UserId,
                    Amount = req.Amount,
                    Status = "Failed",
                    CreatedAtUtc = now
                });

                var ev = new PaymentFailed(
                    MessageId: Guid.NewGuid(),
                    OrderId: req.OrderId,
                    UserId: req.UserId,
                    Amount: req.Amount,
                    Reason: "Недостаточно средств",
                    OccurredAtUtc: now
                );

                db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(PaymentFailed),
                    PayloadJson = JsonSerializer.Serialize(ev),
                    OccurredAtUtc = now,
                    Attempts = 0
                }); 
            }
        }

        // 5) Пишем Inbox в той же транзакции
        db.InboxMessages.Add(new InboxMessage
        {
            MessageId = msgId,
            ProcessedAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (req, msgId);
    }

    private async Task MarkInfrastructureFailure(OrderPaymentRequested? req, BasicDeliverEventArgs ea, int attempts, CancellationToken cancellationToken)
    {
        var type = ea.BasicProperties?.Type;
        var msgIdStr = ea.BasicProperties?.MessageId;

        if (req is null || type != nameof(OrderPaymentRequested) || string.IsNullOrWhiteSpace(msgIdStr) || !Guid.TryParse(msgIdStr, out var msgId))
            throw new InvalidOperationException("Некорректное сообщение для фиксации ошибки инфраструктуры");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var already = await db.InboxMessages.AnyAsync(x => x.MessageId == msgId, cancellationToken);
        if (already)
        {
            await tx.CommitAsync(cancellationToken);
            return;
        }

        var paymentExists = await db.Payments.AnyAsync(x => x.OrderId == req.OrderId, cancellationToken);
        if (!paymentExists)
        {
            db.Payments.Add(new Payment
            {
                OrderId = req.OrderId,
                UserId = req.UserId,
                Amount = req.Amount,
                Status = "Failed",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        var ev = new PaymentFailed(
            MessageId: Guid.NewGuid(),
            OrderId: req.OrderId,
            UserId: req.UserId,
            Amount: req.Amount,
            Reason: $"Техническая ошибка после {attempts} попыток",
            OccurredAtUtc: DateTimeOffset.UtcNow
        );

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(PaymentFailed),
            PayloadJson = JsonSerializer.Serialize(ev),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Attempts = 0
        });

        db.InboxMessages.Add(new InboxMessage
        {
            MessageId = msgId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogWarning("Отметили заказ {OrderId} как неуспешный из-за инфраструктурной ошибки после {Attempts} попыток", req.OrderId, attempts);
    }
}
