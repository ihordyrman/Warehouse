using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Analyzer.Backend.Okx.Messages;
using Microsoft.Extensions.Options;

// ReSharper disable InconsistentNaming

namespace Analyzer.Backend.Okx;

public class OkxClient(IOptions<OkxConfiguration> okxConfiguration, ILogger<OkxClient> logger) : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly ClientWebSocket webSocket = new();
    private Task? heartBeat;

    public event Action OnLoginSuccess;

    public event Action<string> OnError;

    public event Action<object> OnDataReceived;

    public async Task SubscribeToChannelAsync(string channel, string instId)
    {
        var subscribeRequest = new
        {
            op = SocketOperations.Subscribe,
            args = new[]
            {
                new
                {
                    channel, instId
                }
            }
        };

        await SendMessageAsync(subscribeRequest);
    }

    private string GenerateSignature(string timestamp)
    {
        byte[] sign = Encoding.UTF8.GetBytes($"{timestamp}GET/users/self/verify");
        byte[] secretKey = Encoding.UTF8.GetBytes(okxConfiguration.Value.SecretKey);

        using var hmac = new HMACSHA256(secretKey);
        byte[] hash = hmac.ComputeHash(sign);
        return Convert.ToBase64String(hash);
    }

    private async Task LoginAsync()
    {
        string timestamp = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000).ToString();
        string sign = GenerateSignature(timestamp);

        var loginRequest = new
        {
            op = SocketOperations.Login,
            args = new[]
            {
                new
                {
                    apiKey = okxConfiguration.Value.ApiKey,
                    passphrase = okxConfiguration.Value.Passphrase,
                    timestamp,
                    sign
                }
            }
        };

        await SendMessageAsync(loginRequest);
    }

    private async Task SendMessageAsync(object message)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            logger.LogError("Unable to send message. WebSocket is closed");
            throw new InvalidOperationException("WebSocket is not connected");
        }

        string json = JsonSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
        logger.LogDebug("Message sent");
    }

    public async Task ConnectAsync(ChannelType channelType = ChannelType.Public)
    {
        try
        {
            string url = GetWebSocketUrl(channelType);
            await webSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);

            heartBeat = LaunchHeartBeat();
            logger.LogInformation("Connected to {Url}", url);
            _ = Task.Run(async () => await ListenForMessages());

            if (channelType == ChannelType.Private)
            {
                await LoginAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Connection error: {Message}", ex.Message);
            throw;
        }
    }

    private async Task LaunchHeartBeat()
    {
        PeriodicTimer periodicTimer = new(TimeSpan.FromSeconds(10));
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            await SendHeartBeatAsync();
            await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token);
        }

        async Task SendHeartBeatAsync()
        {
            if (webSocket.State != WebSocketState.Open)
            {
                logger.LogError("Unable to send ping message. WebSocket is closed");
                return;
            }

            byte[] bytes = "ping"u8.ToArray();
            var buffer = new ArraySegment<byte>(bytes);

            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
            logger.LogInformation("Sent ping message");
        }
    }

    private async Task ListenForMessages()
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    HandleMessage(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error receiving message: {Message}", ex.Message);
            }
        }
    }

    private string GetWebSocketUrl(ChannelType channelType)
        => channelType switch
        {
            ChannelType.Public => okxConfiguration.Value.IsDemo ? SocketEndpoints.DEMO_PUBLIC_WS_URL : SocketEndpoints.PUBLIC_WS_URL,
            ChannelType.Private => okxConfiguration.Value.IsDemo ? SocketEndpoints.DEMO_PRIVATE_WS_URL : SocketEndpoints.PRIVATE_WS_URL,
            ChannelType.Business => okxConfiguration.Value.IsDemo ? SocketEndpoints.DEMO_BUSINESS_WS_URL : SocketEndpoints.BUSINESS_WS_URL,
            _ => SocketEndpoints.DEMO_PUBLIC_WS_URL
        };

    private void HandleMessage(string message)
    {
        if (message == "pong")
        {
            return;
        }

        try
        {
            logger.LogDebug("Received: {Message}", message);
            OkxEventResponse? eventResponse = JsonSerializer.Deserialize<OkxEventResponse>(message);
            switch (eventResponse!.Event)
            {
                case OkxEvent.Login:
                    logger.LogInformation("Logged in successfully.");
                    break;
                case OkxEvent.Error:
                    logger.LogError("Something bad happened.");
                    break;
                default:
                    logger.LogInformation("Unknown message type");
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error parsing message: {Message}", ex.Message);
        }
    }

    internal static class SocketEndpoints
    {
        internal const string PUBLIC_WS_URL = "wss://ws.okx.com:8443/ws/v5/public";
        internal const string PRIVATE_WS_URL = "wss://ws.okx.com:8443/ws/v5/private";
        internal const string BUSINESS_WS_URL = "wss://ws.okx.com:8443/ws/v5/business";

        internal const string DEMO_PUBLIC_WS_URL = "wss://wspap.okx.com:8443/ws/v5/public";
        internal const string DEMO_PRIVATE_WS_URL = "wss://wspap.okx.com:8443/ws/v5/private";
        internal const string DEMO_BUSINESS_WS_URL = "wss://wspap.okx.com:8443/ws/v5/business";
    }

    internal static class SocketOperations
    {
        internal const string Login = "login";
        internal const string Subscribe = "Subscribe";
        internal const string Unsubscribe = "unsubscribe";
    }

    internal static class InstrumentType
    {
        internal const string Spot = "SPOT";
        internal const string Margin = "MARGIN";
        internal const string Swap = "SWAP";
        internal const string Futures = "FUTURES";
        internal const string Option = "OPTION";
        internal const string Any = "ANY";
    }

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        webSocket.Dispose();
        heartBeat?.Dispose();
    }
}

public enum ChannelType
{
    Public,
    Private,
    Business
}
