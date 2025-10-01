using System.Net.WebSockets;
using Warehouse.Core.Abstractions.Markets;
using Warehouse.Core.Infrastructure;

namespace Warehouse.Backend.Markets.Okx.Services;

internal sealed class OkxConnectionManager : IDisposable
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly OkxHeartbeatService heartbeatService;
    private readonly ILogger<OkxConnectionManager> logger;
    private readonly TimeSpan reconnectDelay = TimeSpan.FromSeconds(5);
    private readonly IWebSocketClient webSocketClient;
    private bool disposed;
    private CancellationTokenSource? reconnectCts;
    private Task? reconnectTask;

    public OkxConnectionManager(
        IWebSocketClient webSocketClient,
        OkxHeartbeatService heartbeatService,
        ILogger<OkxConnectionManager> logger)
    {
        this.webSocketClient = webSocketClient;
        this.heartbeatService = heartbeatService;
        this.logger = logger;

        this.webSocketClient.StateChanged += OnStateChanged;
    }

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        StopReconnect();
        connectionLock.Dispose();
        webSocketClient.StateChanged -= OnStateChanged;
    }

    public event EventHandler<ConnectionState>? StateChanged;

    public async Task<bool> ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (State == ConnectionState.Connected)
            {
                return true;
            }

            State = ConnectionState.Connecting;
            StateChanged?.Invoke(this, State);

            await webSocketClient.ConnectAsync(uri, cancellationToken);
            heartbeatService.Start(webSocketClient);

            State = ConnectionState.Connected;
            StateChanged?.Invoke(this, State);

            logger.LogInformation("Connected to {Uri}", uri);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect");
            State = ConnectionState.Failed;
            StateChanged?.Invoke(this, State);

            StartReconnect(uri);
            return false;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            StopReconnect();
            heartbeatService.Stop();

            if (webSocketClient.State == WebSocketState.Open)
            {
                await webSocketClient.DisconnectAsync("User requested disconnect", cancellationToken);
            }

            State = ConnectionState.Disconnected;
            StateChanged?.Invoke(this, State);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (State != ConnectionState.Connected)
        {
            throw new InvalidOperationException($"Cannot send message in state: {State}");
        }

        await webSocketClient.SendAsync(message, cancellationToken);
    }

    private void OnStateChanged(object? sender, WebSocketState state)
    {
        if (state == WebSocketState.Closed || state == WebSocketState.Aborted)
        {
            State = ConnectionState.Disconnected;
            StateChanged?.Invoke(this, State);
        }
    }

    private void StartReconnect(Uri uri)
    {
        StopReconnect();

        reconnectCts = new CancellationTokenSource();
        reconnectTask = Task.Run(
            async () =>
            {
                while (!reconnectCts.Token.IsCancellationRequested)
                {
                    logger.LogInformation("Attempting reconnection in {Delay}...", reconnectDelay);
                    await Task.Delay(reconnectDelay, reconnectCts.Token);

                    if (await ConnectAsync(uri, reconnectCts.Token))
                    {
                        logger.LogInformation("Reconnection successful");
                        break;
                    }
                }
            },
            reconnectCts.Token);
    }

    private void StopReconnect()
    {
        reconnectCts?.Cancel();
        reconnectCts?.Dispose();
        reconnectCts = null;
        reconnectTask?.Dispose();
        reconnectTask = null;
    }
}
