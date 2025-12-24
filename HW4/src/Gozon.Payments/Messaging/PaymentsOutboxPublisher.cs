using System.Text;
using Gozon.Contracts.Messaging;
using Gozon.Payments.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Gozon.Payments.Messaging;

public sealed class PaymentsOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _rmq;
    private readonly RabbitMqOptions _opt;
    private readonly ILogger<PaymentsOutboxPublisher> _logger;

    public PaymentsOutboxPublisher(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory rmq,
        IOptions<RabbitMqOptions> opt,
        ILogger<PaymentsOutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _rmq = rmq;
        _opt = opt.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = _rmq.GetConnection().CreateModel();

        channel.ExchangeDeclare(_opt.Exchange, type: "topic", durable: true, autoDelete: false);
        channel.ConfirmSelect();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                // Публикуем накопленные события оплаты из outbox.
                var batch = await db.OutboxMessages
                    .Where(x => x.PublishedAtUtc == null)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                foreach (var msg in batch)
                {
                    try
                    {
                        if (msg.Attempts > 0 && msg.LastError is not null)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, msg.Attempts), 30));
                            await Task.Delay(delay, stoppingToken);
                        }

                        var routingKey = TopicResolver.ForMessageType(msg.Type);

                        var props = channel.CreateBasicProperties();
                        props.MessageId = msg.Id.ToString();
                        props.Type = msg.Type;
                        props.ContentType = "application/json";
                        props.DeliveryMode = 2;

                        var body = Encoding.UTF8.GetBytes(msg.PayloadJson);

                        channel.BasicPublish(
                            exchange: _opt.Exchange,
                            routingKey: routingKey,
                            basicProperties: props,
                            body: body);

                        var confirmed = channel.WaitForConfirms(TimeSpan.FromSeconds(5));
                        if (!confirmed)
                            throw new InvalidOperationException($"Брокер не подтвердил публикацию {msg.Id}");

                        msg.PublishedAtUtc = DateTimeOffset.UtcNow;
                        msg.LastError = null;
                    }
                    catch (Exception ex)
                    {
                        msg.Attempts += 1;
                        msg.LastError = ex.Message;
                        _logger.LogError(ex, "Не удалось опубликовать outbox {Id} {Type}", msg.Id, msg.Type);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Цикл публикации outbox завершился с ошибкой");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
