using Warehouse.Backend.Core.Abstractions.Markets;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Markets.Okx.Constants;
using Warehouse.Backend.Markets.Okx.Services;

namespace Warehouse.Backend.Markets.Okx;

internal sealed class OkxMarketAdapter : IMarketAdapter, IDisposable
{
    private readonly OkxConnectionManager connectionManager;
    private readonly MarketCredentials credentials;
    private readonly ReaderWriterLockSlim dataLock = new(LockRecursionPolicy.NoRecursion);
    private readonly OkxHttpService httpService;
    private readonly ILogger<OkxMarketAdapter> logger;
    private readonly OkxMessageProcessor messageProcessor;
    private readonly OkxSubscriptionManager subscriptionManager;
    private readonly IWebSocketClient webSocketClient;
    private bool disposed;

    public OkxMarketAdapter(
        IServiceScopeFactory scopeFactory,
        IMarketDataCache dataCache,
        IWebSocketClient webSocketClient,
        OkxHeartbeatService heartbeatService,
        OkxHttpService httpService,
        ILoggerFactory loggerFactory)
    {
        this.webSocketClient = webSocketClient;
        this.httpService = httpService;
        logger = loggerFactory.CreateLogger<OkxMarketAdapter>();

        IServiceScope scope = scopeFactory.CreateScope();
        credentials = scope.ServiceProvider.GetService<ICredentialsProvider>()!.GetCredentialsAsync(MarketType.Okx)
            .GetAwaiter()
            .GetResult();

        connectionManager = new OkxConnectionManager(webSocketClient, heartbeatService, loggerFactory.CreateLogger<OkxConnectionManager>());
        messageProcessor = new OkxMessageProcessor(loggerFactory.CreateLogger<OkxMessageProcessor>(), dataCache);
        subscriptionManager = new OkxSubscriptionManager(connectionManager, loggerFactory.CreateLogger<OkxSubscriptionManager>());

        this.webSocketClient.MessageReceived += OnWebSocketMessage;
        connectionManager.StateChanged += OnConnectionStateChanged;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        webSocketClient.MessageReceived -= OnWebSocketMessage;
        connectionManager.StateChanged -= OnConnectionStateChanged;

        connectionManager.Dispose();
        dataLock.Dispose();
    }

    public MarketType MarketType => MarketType.Okx;

    public ConnectionState ConnectionState => connectionManager.State;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (ConnectionState == ConnectionState.Connected)
        {
            logger.LogDebug("Already connected");
            return true;
        }

        try
        {
            httpService.Configure(credentials!);

            Uri uri = GetConnectionUri(OkxChannelType.Public);
            bool connected = await connectionManager.ConnectAsync(uri, cancellationToken);

            if (!connected)
            {
                return false;
            }

            if (credentials.ApiKey != null)
            {
                await AuthenticateAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to OKX");
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await subscriptionManager.UnsubscribeAllAsync(cancellationToken);
        await connectionManager.DisconnectAsync(cancellationToken);
    }

    public async Task SubscribeAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (ConnectionState != ConnectionState.Connected)
        {
            throw new InvalidOperationException($"Cannot subscribe in state: {ConnectionState}");
        }

        await subscriptionManager.SubscribeAsync("books", symbol, cancellationToken);
    }

    public async Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await subscriptionManager.UnsubscribeAsync("books", symbol, cancellationToken);

        dataLock.EnterWriteLock();
        try
        {
            // todo: clean data for a symbol from cache
        }
        finally
        {
            dataLock.ExitWriteLock();
        }
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        object authRequest = OkxAuthService.CreateAuthRequest(credentials!);
        await connectionManager.SendAsync(authRequest, cancellationToken);
        logger.LogInformation("Authentication request sent");
    }

    private async void OnWebSocketMessage(object? sender, WebSocketMessage message)
    {
        try
        {
            await messageProcessor.ProcessMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
        => logger.LogInformation("Connection state changed to: {State}", state);

    private static Uri GetConnectionUri(OkxChannelType channelType)
        => new(
            channelType switch
            {
                OkxChannelType.Public => SocketEndpoints.PUBLIC_WS_URL,
                OkxChannelType.Private => SocketEndpoints.PRIVATE_WS_URL,
                _ => throw new ArgumentException($"Invalid channel type: {channelType}")
            });
}
