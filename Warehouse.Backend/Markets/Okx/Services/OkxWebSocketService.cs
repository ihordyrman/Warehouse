using System.Net.WebSockets;
using System.Text.Json;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Markets.Okx.Constants;
using Warehouse.Backend.Markets.Okx.Messages;
using Warehouse.Backend.Markets.Okx.Messages.Socket;
using WebSocketError = Warehouse.Backend.Core.Infrastructure.WebSocketError;

namespace Warehouse.Backend.Markets.Okx.Services;

internal class OkxWebSocketService : IDisposable
{
    private readonly OkxHeartbeatService heartbeatService;
    private readonly ILogger<OkxWebSocketService> logger;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly IWebSocketClient webSocketClient;
    private static readonly SemaphoreSlim Semaphore = new(1);
    private bool disposed;

    public event EventHandler<MarketData>? MarketDataReceived;

    public OkxWebSocketService(IWebSocketClient webSocketClient, OkxHeartbeatService heartbeatService, ILogger<OkxWebSocketService> logger)
    {
        this.webSocketClient = webSocketClient;
        this.heartbeatService = heartbeatService;
        this.logger = logger;
        serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = OkxJsonContext.Default
        };

        this.webSocketClient.MessageReceived += OnMessageReceived;
        this.webSocketClient.ErrorOccurred += OnErrorOccurred;
        this.webSocketClient.StateChanged += OnStateChanged;
    }

    public bool IsConnected { get; private set; }

    public async Task ConnectAsync(
        MarketCredentials credentials,
        OkxChannelType channelType = OkxChannelType.Public,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            if (IsConnected)
            {
                return;
            }

            Uri uri = GetConnectionUri(credentials.IsDemo, channelType);
            await webSocketClient.ConnectAsync(uri, cancellationToken);
            IsConnected = true;

            if (channelType == OkxChannelType.Private)
            {
                await AuthenticateAsync(credentials, cancellationToken);
            }

            heartbeatService.Start(webSocketClient);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        heartbeatService.Stop();
        await webSocketClient.DisconnectAsync(cancellationToken: ct);
    }

    public async Task SubscribeAsync(string channel, string instId, CancellationToken ct = default)
    {
        var request = new
        {
            op = "subscribe",
            args = new[] { new { channel, instId } }
        };

        await webSocketClient.SendAsync(request, ct);
        logger.LogInformation("Subscribed to {Channel} for {InstId}", channel, instId);
    }

    public async Task UnsubscribeAsync(string channel, string instId, CancellationToken ct = default)
    {
        var request = new
        {
            op = "unsubscribe",
            args = new[] { new { channel, instId } }
        };

        await webSocketClient.SendAsync(request, ct);
        logger.LogInformation("Unsubscribed from {Channel} for {InstId}", channel, instId);
    }

    private async Task AuthenticateAsync(MarketCredentials credentials, CancellationToken cancellationToken)
    {
        object authRequest = OkxAuthService.CreateAuthRequest(credentials);
        await webSocketClient.SendAsync(authRequest, cancellationToken);
        logger.LogInformation("Authentication request sent");
    }

    private static Uri GetConnectionUri(bool isDemo, OkxChannelType channelType)
    {
        string baseUrl = (isDemo, channelType) switch
        {
            (true, OkxChannelType.Public) => SocketEndpoints.DEMO_PUBLIC_WS_URL,
            (true, OkxChannelType.Private) => SocketEndpoints.DEMO_PRIVATE_WS_URL,
            (true, OkxChannelType.Business) => SocketEndpoints.DEMO_BUSINESS_WS_URL,
            (false, OkxChannelType.Public) => SocketEndpoints.PUBLIC_WS_URL,
            (false, OkxChannelType.Private) => SocketEndpoints.PRIVATE_WS_URL,
            (false, OkxChannelType.Business) => SocketEndpoints.BUSINESS_WS_URL,
            _ => throw new ArgumentException($"Invalid channel type: {channelType}")
        };

        return new Uri(baseUrl);
    }

    private void OnMessageReceived(object? sender, WebSocketMessage message)
    {
        if (message.Text == null)
        {
            return;
        }

        try
        {
            if (message.Text == "pong")
            {
                logger.LogDebug("Pong received");
                return;
            }

            OkxSocketResponse? okxMessage = JsonSerializer.Deserialize<OkxSocketResponse>(message.Text, serializerOptions);
            if (okxMessage == null)
            {
                return;
            }

            try
            {
                if (okxMessage.Event == OkxEvent.Subscribe)
                {
                    logger.LogInformation(
                        "Successfully subscribed to {Channel}:{Instrument}",
                        okxMessage.Arguments!.Channel,
                        okxMessage.Arguments.InstrumentId);
                    return;
                }

                if (okxMessage.Data?.Length > 0)
                {
                    var marketData = new MarketData(
                        okxMessage.Arguments!.InstrumentId!,
                        okxMessage.Data[0].Asks!,
                        okxMessage.Data[0].Bids!);

                    MarketDataReceived?.Invoke(this, marketData);
                    return;
                }

                logger.LogWarning("Unable to process okx socket message: {Message}", message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message: {Message}", message);
        }
    }

    private void OnErrorOccurred(object? sender, WebSocketError error)
        => logger.LogError(error.Exception, "WebSocket error: {Message}", error.Message);

    private void OnStateChanged(object? sender, WebSocketState state)
        => logger.LogInformation("WebSocket state changed to: {State}", state);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        IsConnected = false;

        heartbeatService.Stop();
        webSocketClient.MessageReceived -= OnMessageReceived;
        webSocketClient.ErrorOccurred -= OnErrorOccurred;
        webSocketClient.StateChanged -= OnStateChanged;
        webSocketClient.Dispose();
    }
}
