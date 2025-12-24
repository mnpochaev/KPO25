using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Gozon.Gateway;

/// <summary>
/// Рассылка уведомлений по WebSocket для пользователей.
/// </summary>
public sealed class WebSocketNotifier
{
    private readonly ConcurrentDictionary<long, ConcurrentBag<WebSocket>> _connections = new();
    private readonly ILogger<WebSocketNotifier> _logger;

    public WebSocketNotifier(ILogger<WebSocketNotifier> logger)
    {
        _logger = logger;
    }

    public void Add(long userId, WebSocket socket)
    {
        var list = _connections.GetOrAdd(userId, _ => new ConcurrentBag<WebSocket>());
        list.Add(socket);
    }

    public async Task BroadcastAsync(long userId, string json, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(userId, out var sockets))
            return;

        var bytes = Encoding.UTF8.GetBytes(json);
        var seg = new ArraySegment<byte>(bytes);
        var toRemove = new List<WebSocket>();

        foreach (var socket in sockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                toRemove.Add(socket);
                continue;
            }

            try
            {
                await socket.SendAsync(seg, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить уведомление пользователю {UserId}", userId);
                toRemove.Add(socket);
            }
        }

        if (toRemove.Count > 0)
        {
            foreach (var sock in toRemove)
            {
                try { sock.Abort(); sock.Dispose(); } catch { /* ignore */ }
            }
            var remaining = new ConcurrentBag<WebSocket>(sockets.Where(s => !toRemove.Contains(s)));
            if (remaining.IsEmpty)
                _connections.TryRemove(userId, out _);
            else
                _connections[userId] = remaining;
        }
    }
}
