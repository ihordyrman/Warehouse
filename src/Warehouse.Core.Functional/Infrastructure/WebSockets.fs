namespace Warehouse.Core.Functional.Infrastructure.WebSockets

open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open System.Buffers
open System.Collections.Generic
open System.Text
open System.Text.Json
open Microsoft.Extensions.Logging

type WebSocketMessage = { Text: string option; Binary: byte[] option; Type: WebSocketMessageType }

type WebSocketError = { Exception: Exception; Message: string }

type IWebSocketClient =
    inherit IDisposable
    abstract member State: WebSocketState

    [<CLIEvent>]
    abstract member MessageReceived: IEvent<WebSocketMessage>

    [<CLIEvent>]
    abstract member ErrorOccurred: IEvent<WebSocketError>

    [<CLIEvent>]
    abstract member StateChanged: IEvent<WebSocketState>

    abstract member ConnectAsync: uri: Uri * ?cancellationToken: CancellationToken -> Task
    abstract member DisconnectAsync: ?reason: string * ?cancellationToken: CancellationToken -> Task
    abstract member SendAsync: message: string * ?cancellationToken: CancellationToken -> Task
    abstract member SendAsync<'T when 'T: not struct> : message: 'T * ?cancellationToken: CancellationToken -> Task


type WebSocketClient(logger: ILogger<WebSocketClient>) =
    let arrayPool = ArrayPool<byte>.Shared
    let sendSemaphore = new SemaphoreSlim(1, 1)
    let webSocket = new ClientWebSocket()

    let mutable disposed = false
    let mutable listenCts: CancellationTokenSource option = None
    let mutable listenTask: Task option = None

    let messageReceived = Event<WebSocketMessage>()
    let errorOccurred = Event<WebSocketError>()
    let stateChanged = Event<WebSocketState>()

    let raiseError ex message = errorOccurred.Trigger { Exception = ex; Message = message }

    let sendAsyncInternal (data: byte[]) (messageType: WebSocketMessageType) (ct: CancellationToken) =
        task {
            if webSocket.State <> WebSocketState.Open then
                invalidOp $"Cannot send message. WebSocket state: {webSocket.State}"

            do! sendSemaphore.WaitAsync(ct)

            try
                do! webSocket.SendAsync(ArraySegment<byte>(data), messageType, true, ct)
                logger.LogDebug("Message sent ({Size} bytes)", data.Length)
            finally
                sendSemaphore.Release() |> ignore
        }

    let handleCloseAsync (ct: CancellationToken) =
        task {
            do! webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct)
            stateChanged.Trigger WebSocketState.Closed
            logger.LogInformation("WebSocket connection closed")
        }

    let processMessage (result: WebSocketReceiveResult) (buffer: byte[]) (chunks: List<byte[] * int>) =
        if not result.EndOfMessage then
            let chunk = arrayPool.Rent(result.Count)
            Array.Copy(buffer, 0, chunk, 0, result.Count)
            chunks.Add((chunk, result.Count))
        else
            try
                let messageData =
                    if chunks.Count > 0 then
                        let lastChunk = arrayPool.Rent(result.Count)
                        Array.Copy(buffer, 0, lastChunk, 0, result.Count)
                        chunks.Add((lastChunk, result.Count))

                        let totalLength = chunks |> Seq.sumBy snd
                        let data = Array.zeroCreate<byte> totalLength

                        let mutable position = 0

                        for (array, length) in chunks do
                            Array.Copy(array, 0, data, position, length)
                            position <- position + length

                        data
                    else
                        let data = Array.zeroCreate<byte> result.Count
                        Array.Copy(buffer, 0, data, 0, result.Count)
                        data

                let message =
                    { Type = result.MessageType
                      Binary = if result.MessageType = WebSocketMessageType.Binary then Some messageData else None
                      Text =
                        if result.MessageType = WebSocketMessageType.Text then
                            Some(Encoding.UTF8.GetString(messageData))
                        else
                            None }

                messageReceived.Trigger message
            finally
                for array, _ in chunks do
                    arrayPool.Return(array)

                chunks.Clear()

    let listenAsync (ct: CancellationToken) =
        task {
            let buffer = arrayPool.Rent(4096)
            let chunks = List<byte[] * int>()

            try
                while webSocket.State = WebSocketState.Open && not ct.IsCancellationRequested do
                    try
                        let! result = webSocket.ReceiveAsync(ArraySegment<byte>(buffer), ct)

                        if result.MessageType = WebSocketMessageType.Close then
                            do! handleCloseAsync ct
                        else
                            processMessage result buffer chunks
                    with
                    | :? OperationCanceledException -> ()
                    | ex ->
                        logger.LogError(ex, "Error receiving message")
                        raiseError ex ex.Message
            finally
                arrayPool.Return(buffer)

                for array, _ in chunks do
                    arrayPool.Return(array)
        }

    member _.State = webSocket.State

    [<CLIEvent>]
    member _.MessageReceived = messageReceived.Publish

    [<CLIEvent>]
    member _.ErrorOccurred = errorOccurred.Publish

    [<CLIEvent>]
    member _.StateChanged = stateChanged.Publish

    member _.ConnectAsync(uri: Uri, ?cancellationToken: CancellationToken) =
        task {
            if webSocket.State = WebSocketState.Open then
                invalidOp "WebSocket is already connected"

            try
                let ct = defaultArg cancellationToken CancellationToken.None

                do! webSocket.ConnectAsync(uri, ct)
                logger.LogInformation("Connected to {Uri}", uri)
                stateChanged.Trigger WebSocketState.Open

                let cts = new CancellationTokenSource()
                listenCts <- Some cts
                listenTask <- Some(Task.Run(Func<Task>(fun () -> listenAsync cts.Token), cts.Token))
            with ex ->
                logger.LogError(ex, "Failed to connect to {Uri}", uri)
                raiseError ex ex.Message
        }

    member _.DisconnectAsync(?reason: string, ?cancellationToken: CancellationToken) =
        let ct = defaultArg cancellationToken CancellationToken.None

        task {
            if webSocket.State = WebSocketState.Open then
                try
                    let closeReason = defaultArg reason "Closing connection"
                    do! webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeReason, ct)

                    match listenCts with
                    | Some cts -> do! cts.CancelAsync()
                    | None -> ()

                    match listenTask with
                    | Some t -> do! t
                    | None -> ()

                    stateChanged.Trigger WebSocketState.Closed
                with ex ->
                    logger.LogError(ex, "Error during disconnect")
                    raiseError ex ex.Message
        }

    member this.SendAsync(message: string, ?cancellationToken: CancellationToken) =
        let ct = defaultArg cancellationToken CancellationToken.None
        let bytes = Encoding.UTF8.GetBytes(message)
        sendAsyncInternal bytes WebSocketMessageType.Text ct

    member this.SendAsync<'T when 'T: not struct>(message: 'T, ?cancellationToken: CancellationToken) =
        let ct = defaultArg cancellationToken CancellationToken.None
        let json = JsonSerializer.Serialize(message)
        this.SendAsync(json, ct)

    interface IWebSocketClient with
        member this.State = this.State

        [<CLIEvent>]
        member this.MessageReceived = this.MessageReceived

        [<CLIEvent>]
        member this.ErrorOccurred = this.ErrorOccurred

        [<CLIEvent>]
        member this.StateChanged = this.StateChanged

        member this.ConnectAsync(uri, ct) =
            let ct = defaultArg ct CancellationToken.None
            this.ConnectAsync(uri, ct)

        member this.DisconnectAsync(reason, ct) =
            let ct = defaultArg ct CancellationToken.None
            let reason = defaultArg reason ""
            this.DisconnectAsync(reason, ct)

        member this.SendAsync(message, ct) =
            match ct with
            | Some token -> this.SendAsync(message, token) :> Task
            | None -> this.SendAsync(message) :> Task

        member this.SendAsync<'T when 'T: not struct>(message: 'T, ct) =
            match ct with
            | Some token -> this.SendAsync<'T>(message, token) :> Task
            | None -> this.SendAsync<'T>(message) :> Task

    interface IDisposable with
        member this.Dispose() =
            if not disposed then
                disposed <- true

                try
                    this.DisconnectAsync().GetAwaiter().GetResult()
                with ex ->
                    logger.LogWarning(ex, "Error during dispose")

                listenCts |> Option.iter _.Dispose()
                webSocket.Dispose()
                sendSemaphore.Dispose()
