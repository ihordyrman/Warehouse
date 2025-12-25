namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core
open Warehouse.Core.Infrastructure.WebSockets
open Warehouse.Core.Markets.Contracts
open Warehouse.Core.Markets.Domain
open Warehouse.Core.Markets.Concrete.Okx.Constants
open Warehouse.Core.Markets.Concrete.Okx.Services

type OkxChannelType =
    | Public
    | Private

type OkxMarketAdapter
    (
        scopeFactory: IServiceScopeFactory,
        dataCache: IMarketDataCache,
        webSocketClient: IWebSocketClient,
        heartbeatService: OkxHeartbeatService,
        loggerFactory: ILoggerFactory
    ) =

    let logger = loggerFactory.CreateLogger<OkxMarketAdapter>()

    let credentials =
        use scope = scopeFactory.CreateScope()
        let store = CompositionRoot.createCredentialsStore scope.ServiceProvider

        store.GetCredentials MarketType.Okx CancellationToken.None
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> function
            | Ok creds -> Some creds
            | Error err ->
                logger.LogWarning("No credentials found for OKX: {Error}", err)
                None

    let connectionManager =
        new OkxConnectionManager(webSocketClient, heartbeatService, loggerFactory.CreateLogger<OkxConnectionManager>())

    let messageProcessor = OkxMessageProcessor(loggerFactory.CreateLogger<OkxMessageProcessor>(), dataCache)

    let subscriptionManager =
        OkxSubscriptionManager(connectionManager, loggerFactory.CreateLogger<OkxSubscriptionManager>())

    let dataLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion)

    let mutable disposed = false

    let onWebSocketMessage (_: obj) (message: WebSocketMessage) =
        try
            messageProcessor.ProcessMessage(message)
        with ex ->
            logger.LogError(ex, "Error processing WebSocket message: {Message}", message)

    let onConnectionStateChanged (_: obj) (state: ConnectionState) =
        logger.LogInformation("Connection state changed to: {State}", state)

    let webSocketMessageHandler = Handler<WebSocketMessage>(onWebSocketMessage)
    let connectionStateHandler = EventHandler<ConnectionState>(onConnectionStateChanged)

    do
        webSocketClient.MessageReceived.AddHandler(webSocketMessageHandler)
        connectionManager.StateChanged.AddHandler(connectionStateHandler)

    let getConnectionUri (channelType: OkxChannelType) =
        let endpoints =
            match credentials with
            | None -> SocketEndpoints.PUBLIC_WS_URL
            | Some credentials ->
                if credentials.IsSandbox then
                    match channelType with
                    | Public -> SocketEndpoints.DEMO_PUBLIC_WS_URL
                    | Private -> SocketEndpoints.DEMO_PRIVATE_WS_URL
                else
                    match channelType with
                    | Public -> SocketEndpoints.PUBLIC_WS_URL
                    | Private -> SocketEndpoints.PRIVATE_WS_URL

        Uri(endpoints)

    let authenticateAsync (ct: CancellationToken) =
        task {
            match credentials with
            | Some cr ->
                let authRequest = OkxAuth.createAuthRequest cr
                do! connectionManager.SendAsync(authRequest, ct)
                logger.LogInformation("Authentication request sent")
            | None -> ()
        }

    interface IMarketAdapter with
        member _.MarketType = MarketType.Okx

        member _.ConnectionState = connectionManager.State

        member _.ConnectAsync(cancellationToken) =
            task {
                if connectionManager.State = ConnectionState.Connected then
                    logger.LogDebug("Already connected")
                    return true
                else
                    try
                        let uri = getConnectionUri Public
                        let! connected = connectionManager.ConnectAsync(uri, cancellationToken)

                        if not connected then
                            return false
                        else
                            match credentials with
                            | None -> return false
                            | Some _ ->
                                if not (isNull credentials.Value.ApiKey) then
                                    do! authenticateAsync cancellationToken
                                    return true
                                else
                                    return false
                    with ex ->
                        logger.LogError(ex, "Failed to connect to OKX")
                        return false
            }

        member _.DisconnectAsync(cancellationToken) =
            task {
                do! subscriptionManager.UnsubscribeAllAsync(cancellationToken)
                do! connectionManager.DisconnectAsync(cancellationToken)
            }

        member _.SubscribeAsync(symbol, cancellationToken) =
            task {
                if connectionManager.State <> ConnectionState.Connected then
                    invalidOp $"Cannot subscribe in state: {connectionManager.State}"

                let! _ = subscriptionManager.SubscribeAsync("books", symbol, cancellationToken)
                return ()
            }

        member _.UnsubscribeAsync(symbol, cancellationToken) =
            task {
                let! _ = subscriptionManager.UnsubscribeAsync("books", symbol, cancellationToken)
                dataLock.EnterWriteLock()

                try
                    // todo: clean data for a symbol from cache
                    ()
                finally
                    dataLock.ExitWriteLock()
            }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                webSocketClient.MessageReceived.RemoveHandler(webSocketMessageHandler)
                connectionManager.StateChanged.RemoveHandler(connectionStateHandler)
                (connectionManager :> IDisposable).Dispose()
                dataLock.Dispose()
