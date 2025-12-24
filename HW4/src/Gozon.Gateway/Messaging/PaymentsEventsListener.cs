using System.Text;
using System.Text.Json;
using Gozon.Contracts.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gozon.Gateway.Messaging;

/// <summary>
/// Читает события OrderStatusChanged из брокера и пушит их в WebSocket (по userId).
/// </summary>
public sealed class PaymentsEventsListener : BackgroundService
{
    private const string QueueName = "gozon.gateway.notifications";

    private readonly RabbitMqConnectionFactory _rmq;
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<PaymentsEventsListener> _logger;
    private readonly WebSocketNotifier _notifier;

    public PaymentsEventsListener(
        RabbitMqConnectionFactory rmq,
        IOptions<RabbitMqOptions> opt,
        ILogger<PaymentsEventsListener> logger,
        WebSocketNotifier notifier)
    {
        _rmq = rmq;
        _opt = opt.Value;
        _logger = logger;
        _notifier = notifier;
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
        channel.QueueBind(QueueName, _opt.Exchange, Topics.OrderStatusChanged);

        channel.QueueDeclare(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(dlqName, dlxExchange, dlqName);

        channel.BasicQos(0, 1, false);

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
                    _logger.LogError(ex, "Не удалось отправить уведомление (попытка {Attempt}/{Max})", attempt, maxAttempts);

                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError("Сообщение {MessageId} отправлено в DLQ после {Attempts} попыток", ea.BasicProperties?.MessageId, attempt);
                        channel.BasicNack(ea.DeliveryTag, false, requeue: false);
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
        if (string.IsNullOrWhiteSpace(type))
            return;

        var json = Encoding.UTF8.GetString(ea.Body.ToArray());

        if (type == nameof(OrderStatusChanged))
        {
            var ev = JsonSerializer.Deserialize<OrderStatusChanged>(json);
            if (ev is null) return;
            var payload = JsonSerializer.Serialize(new
            {
                status = ev.Status,
                ev.OrderId,
                ev.OccurredAtUtc
            });

            await _notifier.BroadcastAsync(ev.UserId, payload, cancellationToken);
        }
    }
}
