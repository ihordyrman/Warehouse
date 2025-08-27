using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Warehouse.Backend.Okx.Messages;
using Microsoft.Extensions.Options;
using Warehouse.Backend.Core;
using Warehouse.Backend.Core.Entities;
using Warehouse.Backend.Okx.Constants;
using Warehouse.Backend.Okx.Messages.Socket;
using Core_WebSocketError = Warehouse.Backend.Core.WebSocketError;
using WebSocketError = Warehouse.Backend.Core.WebSocketError;

namespace Warehouse.Backend.Okx.Services;

public class OkxWebSocketService : IDisposable
{
    private readonly IWebSocketClient webSocketClient;
    private readonly OkxHeartbeatService heartbeatService;
    private readonly IOptions<MarketCredentials> config;
    private readonly ILogger<OkxWebSocketService> logger;
    private readonly Channel<OkxSocketResponse> messageChannel;
    private readonly JsonSerializerOptions serializerOptions;
    private OkxChannelType? currentChannelType;
    private bool disposed;

    public OkxWebSocketService(
        IWebSocketClient webSocketClient,
        OkxHeartbeatService heartbeatService,
        IOptions<MarketCredentials> config,
        ILogger<OkxWebSocketService> logger)
    {
        this.webSocketClient = webSocketClient;
        this.heartbeatService = heartbeatService;
        this.config = config;
        this.logger = logger;
        messageChannel = Channel.CreateUnbounded<OkxSocketResponse>();
        serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = OkxJsonContext.Default
        };

        this.webSocketClient.MessageReceived += OnMessageReceived;
        this.webSocketClient.ErrorOccurred += OnErrorOccurred;
        this.webSocketClient.StateChanged += OnStateChanged;
    }

    public async Task ConnectAsync(OkxChannelType channelType, CancellationToken cancellationToken = default)
    {
        Uri uri = GetConnectionUri(channelType);
        await webSocketClient.ConnectAsync(uri, cancellationToken);

        currentChannelType = channelType;

        if (channelType == OkxChannelType.Private)
        {
            await AuthenticateAsync(cancellationToken);
        }

        heartbeatService.Start(webSocketClient);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        heartbeatService.Stop();
        await webSocketClient.DisconnectAsync(cancellationToken: cancellationToken);
        messageChannel.Writer.TryComplete();
    }

    public async Task SubscribeAsync(string channel, string instId, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            op = "subscribe",
            args = new[] { new { channel, instId } }
        };

        await webSocketClient.SendAsync(request, cancellationToken);
        logger.LogInformation("Subscribed to {Channel} for {InstId}", channel, instId);
    }

    public async Task UnsubscribeAsync(string channel, string instId, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            op = "unsubscribe",
            args = new[] { new { channel, instId } }
        };

        await webSocketClient.SendAsync(request, cancellationToken);
        logger.LogInformation("Unsubscribed from {Channel} for {InstId}", channel, instId);
    }

    public IAsyncEnumerable<OkxSocketResponse> GetMessagesAsync(CancellationToken cancellationToken = default)
        => messageChannel.Reader.ReadAllAsync(cancellationToken);

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        object authRequest = OkxAuthService.CreateAuthRequest(config.Value);
        await webSocketClient.SendAsync(authRequest, cancellationToken);
        logger.LogInformation("Authentication request sent");
    }

    private Uri GetConnectionUri(OkxChannelType channelType)
    {
        string baseUrl = (config.Value.IsDemo, channelType) switch
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

            if (okxMessage != null)
            {
                messageChannel.Writer.TryWrite(okxMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message");
        }
    }

    private void OnErrorOccurred(object? sender, Core_WebSocketError error)
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

        heartbeatService.Stop();
        webSocketClient.MessageReceived -= OnMessageReceived;
        webSocketClient.ErrorOccurred -= OnErrorOccurred;
        webSocketClient.StateChanged -= OnStateChanged;
        webSocketClient.Dispose();
        messageChannel.Writer.TryComplete();
    }
}
