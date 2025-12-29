using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Gozon.Gateway.Messaging;

public sealed class RabbitMqConnectionFactory : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private IConnection? _connection;

    public RabbitMqConnectionFactory(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionFactory> logger)
    {
        var o = options.Value;
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = o.Host,
            Port = o.Port,
            UserName = o.User,
            Password = o.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
    }

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        _connection?.Dispose();

        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                _connection = _factory.CreateConnection();
                return _connection;
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
                _logger.LogWarning(ex, "Не удалось подключиться к RabbitMQ (попытка {Attempt}), повтор через {Delay} сек", attempt, delay.TotalSeconds);
                Thread.Sleep(delay);
            }
        }
    }

    public void Dispose() => _connection?.Dispose();
}
