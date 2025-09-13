using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Warehouse.Backend.Core.Infrastructure;

internal interface IWebSocketClient : IDisposable
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

internal sealed class WebSocketClient(ILogger<WebSocketClient> logger) : IWebSocketClient
{
    private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;
    private readonly SemaphoreSlim sendSemaphore = new(1, 1);
    private readonly ClientWebSocket webSocket = new();
    private bool disposed;
    private CancellationTokenSource? listenCts;
    private Task? listenTask;

    public WebSocketState State => webSocket.State;

    public event EventHandler<WebSocketMessage>? MessageReceived;

    public event EventHandler<WebSocketError>? ErrorOccurred;

    public event EventHandler<WebSocketState>? StateChanged;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is already connected");
        }

        try
        {
            await webSocket.ConnectAsync(uri, cancellationToken);
            logger.LogInformation("Connected to {Uri}", uri);

            StateChanged?.Invoke(this, WebSocketState.Open);

            listenCts = new CancellationTokenSource();
            listenTask = Task.Run(() => ListenAsync(listenCts.Token), listenCts.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to {Uri}", uri);
            ErrorOccurred?.Invoke(this, new WebSocketError { Exception = ex, Message = ex.Message });
            throw;
        }
    }

    public async Task DisconnectAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason ?? "Closing connection", cancellationToken);

            await listenCts?.CancelAsync()!;

            if (listenTask != null)
            {
                await listenTask;
            }

            StateChanged?.Invoke(this, WebSocketState.Closed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during disconnect");
            ErrorOccurred?.Invoke(this, new WebSocketError { Exception = ex, Message = ex.Message });
        }
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        string json = JsonSerializer.Serialize(message);
        await SendAsync(json, cancellationToken);
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        await SendAsync(bytes, WebSocketMessageType.Text, cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during dispose");
        }

        listenCts?.Dispose();
        webSocket.Dispose();
        sendSemaphore.Dispose();
    }

    private async Task SendAsync(byte[] data, WebSocketMessageType messageType, CancellationToken cancellationToken)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"Cannot send message. WebSocket state: {webSocket.State}");
        }

        await sendSemaphore.WaitAsync(cancellationToken);
        try
        {
            await webSocket.SendAsync(new ArraySegment<byte>(data), messageType, true, cancellationToken);
            logger.LogDebug("Message sent ({Size} bytes)", data.Length);
        }
        finally
        {
            sendSemaphore.Release();
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = arrayPool.Rent(4096);
        var chunks = new List<(byte[] array, int length)>();

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleCloseAsync(cancellationToken);
                        break;
                    }

                    ProcessMessage(result, buffer, chunks);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error receiving message");
                    ErrorOccurred?.Invoke(this, new WebSocketError { Exception = ex, Message = ex.Message });
                }
            }
        }
        finally
        {
            arrayPool.Return(buffer);
            foreach ((byte[] array, int _) in chunks)
            {
                arrayPool.Return(array);
            }
        }
    }

    private void ProcessMessage(WebSocketReceiveResult result, byte[] buffer, List<(byte[] array, int length)> chunks)
    {
        if (!result.EndOfMessage)
        {
            byte[] chunk = arrayPool.Rent(result.Count);
            Array.Copy(buffer, 0, chunk, 0, result.Count);
            chunks.Add((chunk, result.Count));
            return;
        }

        try
        {
            byte[]? messageData;
            if (chunks.Count > 0)
            {
                chunks.Add((arrayPool.Rent(result.Count), result.Count));
                Array.Copy(buffer, 0, chunks[^1].array, 0, result.Count);

                int totalLength = chunks.Sum(c => c.length);
                messageData = new byte[totalLength];

                int position = 0;
                foreach ((byte[] array, int length) in chunks)
                {
                    Array.Copy(array, 0, messageData, position, length);
                    position += length;
                }
            }
            else
            {
                messageData = new byte[result.Count];
                Array.Copy(buffer, 0, messageData, 0, result.Count);
            }

            var message = new WebSocketMessage
            {
                Type = result.MessageType,
                Binary = result.MessageType == WebSocketMessageType.Binary ? messageData : null,
                Text = result.MessageType == WebSocketMessageType.Text ? Encoding.UTF8.GetString(messageData) : null
            };

            MessageReceived?.Invoke(this, message);
        }
        finally
        {
            foreach ((byte[] array, int _) in chunks)
            {
                arrayPool.Return(array);
            }

            chunks.Clear();
        }
    }

    private async Task HandleCloseAsync(CancellationToken cancellationToken)
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        StateChanged?.Invoke(this, WebSocketState.Closed);
        logger.LogInformation("WebSocket connection closed");
    }
}
