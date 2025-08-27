using System.Net.WebSockets;

namespace Warehouse.Backend.Core;

public interface IWebSocketClient : IDisposable
{
    WebSocketState State { get; }

    event EventHandler<WebSocketMessage>? MessageReceived;

    event EventHandler<WebSocketError>? ErrorOccurred;

    event EventHandler<WebSocketState>? StateChanged;

    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);

    Task DisconnectAsync(string? reason = null, CancellationToken cancellationToken = default);

    Task SendAsync(string message, CancellationToken cancellationToken = default);

    Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;
}
