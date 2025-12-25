namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Warehouse.Core.Infrastructure.WebSockets
open Warehouse.Core.Markets.Domain

type OkxConnectionManager
    (webSocketClient: IWebSocketClient, heartbeatService: OkxHeartbeatService, logger: ILogger<OkxConnectionManager>) as this
    =
    let connectionLock = new SemaphoreSlim(1, 1)
    let reconnectDelay = TimeSpan.FromSeconds(5.0)
    let mutable disposed = false
    let mutable reconnectCts: CancellationTokenSource option = None
    let mutable reconnectTask: Task option = None
    let mutable state = ConnectionState.Disconnected
    let stateChanged = Event<EventHandler<ConnectionState>, ConnectionState>()

    let onStateChanged (_: obj) (socketClientState: WebSocketState) =
        if socketClientState = WebSocketState.Closed || socketClientState = WebSocketState.Aborted then
            state <- ConnectionState.Disconnected
            stateChanged.Trigger(this, state)

    do webSocketClient.StateChanged.AddHandler(Handler<WebSocketState>(onStateChanged))

    member this.State = state

    [<CLIEvent>]
    member this.StateChanged = stateChanged.Publish

    member private this.StopReconnect() =
        reconnectCts
        |> Option.iter (fun c ->
            c.Cancel()
            c.Dispose()
        )

        reconnectCts <- None

        reconnectTask
        |> Option.iter (fun t ->
            try
                t.Dispose()
            with _ ->
                ()
        )

        reconnectTask <- None

    member private this.StartReconnect(uri: Uri) =
        this.StopReconnect()
        let cts = new CancellationTokenSource()
        reconnectCts <- Some cts

        reconnectTask <-
            Some(
                task {
                    while not cts.Token.IsCancellationRequested do
                        logger.LogInformation("Attempting reconnection in {Delay}...", reconnectDelay)
                        do! Task.Delay(reconnectDelay, cts.Token)
                        let! connected = this.ConnectAsync(uri, cts.Token)

                        if connected then
                            logger.LogInformation("Reconnection successful")
                            cts.Cancel()
                }
            )

    member this.ConnectAsync(uri: Uri, ?cancellationToken: CancellationToken) =
        task {
            let ct = defaultArg cancellationToken CancellationToken.None
            do! connectionLock.WaitAsync(ct)

            try
                if state = ConnectionState.Connected then
                    return true
                else
                    state <- ConnectionState.Connecting
                    stateChanged.Trigger(this, state)

                    try
                        do! webSocketClient.ConnectAsync(uri, ct)
                        heartbeatService.Start(webSocketClient)

                        state <- ConnectionState.Connected
                        stateChanged.Trigger(this, state)
                        logger.LogInformation("Connected to {Uri}", uri)
                        return true
                    with ex ->
                        logger.LogError(ex, "Failed to connect")
                        state <- ConnectionState.Failed
                        stateChanged.Trigger(this, state)
                        this.StartReconnect(uri)
                        return false
            finally
                connectionLock.Release() |> ignore
        }

    member this.DisconnectAsync(?cancellationToken: CancellationToken) =
        task {
            let ct = defaultArg cancellationToken CancellationToken.None
            do! connectionLock.WaitAsync(ct)

            try
                this.StopReconnect()
                heartbeatService.Stop()

                if webSocketClient.State = WebSocketState.Open then
                    do! webSocketClient.DisconnectAsync("User requested disconnect", ct)

                state <- ConnectionState.Disconnected
                stateChanged.Trigger(this, state)
            finally
                connectionLock.Release() |> ignore
        }

    member this.SendAsync<'T when 'T: not struct>(message: 'T, ?cancellationToken: CancellationToken) =
        task {
            let ct = defaultArg cancellationToken CancellationToken.None

            if state <> ConnectionState.Connected then
                raise (InvalidOperationException($"Cannot send message in state: {state}"))

            do! webSocketClient.SendAsync(message, ct)
        }

    interface IDisposable with
        member this.Dispose() =
            if not disposed then
                disposed <- true
                this.StopReconnect()
                connectionLock.Dispose()
                webSocketClient.StateChanged.RemoveHandler(Handler<WebSocketState>(onStateChanged))
