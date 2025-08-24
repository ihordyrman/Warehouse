using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Analyzer.Backend.Okx.Configurations;
using Analyzer.Backend.Okx.Constants;
using Analyzer.Backend.Okx.Processors;
using Microsoft.Extensions.Options;

namespace Analyzer.Backend.Okx;

public class OkxWsClient(
    [FromKeyedServices(OkxChannelNames.RawMessages)] Channel<string> messageChannel,
    IOptions<OkxAuthConfiguration> okxAuthConfiguration,
    ILogger<OkxWsClient> logger) : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly OkxAuthConfiguration okxAuthConfiguration = okxAuthConfiguration.Value;
    private readonly byte[] ping = "ping"u8.ToArray();
    private readonly ClientWebSocket webSocket = new();

    private readonly ChannelWriter<string> writer = messageChannel.Writer;
    private Task? heartBeat;

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        webSocket.Dispose();
        heartBeat?.Dispose();
    }

    public async Task SubscribeToChannelAsync(string channel, string instId)
    {
        var subscribeRequest = new
        {
            op = SocketOperations.Subscribe,
            args = new[]
            {
                new
                {
                    channel,
                    instId
                }
            }
        };

        await SendMessageAsync(subscribeRequest);
    }

    private string GenerateSignature(string timestamp)
    {
        byte[] sign = Encoding.UTF8.GetBytes($"{timestamp}GET/users/self/verify");
        byte[] secretKey = Encoding.UTF8.GetBytes(okxAuthConfiguration.SecretKey);

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
                    apiKey = okxAuthConfiguration.ApiKey,
                    passphrase = okxAuthConfiguration.Passphrase,
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

    public async Task ConnectAsync(OkxChannelType okxChannelType)
    {
        try
        {
            string url = GetConnectionUrl(okxChannelType);
            await webSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);

            logger.LogInformation("Connected to {Url}", url);
            _ = Task.Run(async () => await ListenForMessages(), cancellationTokenSource.Token);

            if (okxChannelType == OkxChannelType.Private)
            {
                await LoginAsync();
            }

            heartBeat ??= LaunchHeartBeat();
        }
        catch (Exception ex)
        {
            logger.LogError("Connection error: {Message}", ex.Message);
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

        return;

        async Task SendHeartBeatAsync()
        {
            if (webSocket.State != WebSocketState.Open)
            {
                logger.LogError("Unable to send ping message. WebSocket is closed");
                return;
            }

            var buffer = new ArraySegment<byte>(ping);
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationTokenSource.Token);

            logger.LogInformation("Sent ping message");
        }
    }

    private async Task ListenForMessages()
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);

        while (webSocket.State == WebSocketState.Open && !cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
                    await HandleMessage(message);
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

    private string GetConnectionUrl(OkxChannelType okxChannelType)
        => okxChannelType switch
        {
            OkxChannelType.Public => okxAuthConfiguration.IsDemo ? SocketEndpoints.DEMO_PUBLIC_WS_URL : SocketEndpoints.PUBLIC_WS_URL,
            OkxChannelType.Private => okxAuthConfiguration.IsDemo ? SocketEndpoints.DEMO_PRIVATE_WS_URL : SocketEndpoints.PRIVATE_WS_URL,
            OkxChannelType.Business => okxAuthConfiguration.IsDemo ? SocketEndpoints.DEMO_BUSINESS_WS_URL : SocketEndpoints.BUSINESS_WS_URL,
            _ => SocketEndpoints.DEMO_PUBLIC_WS_URL
        };

    private async Task HandleMessage(string message)
    {
        if (message == "pong")
        {
            logger.LogDebug("Ping response received");
            return;
        }

        try
        {
            await writer.WriteAsync(message, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing message: {Message}", ex.Message);
        }
    }
}
