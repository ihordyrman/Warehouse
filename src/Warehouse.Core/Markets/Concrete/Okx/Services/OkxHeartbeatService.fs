namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Warehouse.Core.Functional.Infrastructure.WebSockets

type OkxHeartbeatService(logger: ILogger<OkxHeartbeatService>) =
    let timer = new PeriodicTimer(TimeSpan.FromSeconds(10.0))
    let mutable cts: CancellationTokenSource option = None
    let mutable heartbeatTask: Task option = None

    member this.Start(client: IWebSocketClient) =
        this.Stop()
        let source = new CancellationTokenSource()
        cts <- Some source

        let loop =
            task {
                try
                    while not source.Token.IsCancellationRequested do
                        try
                            match! timer.WaitForNextTickAsync source.Token with
                            | false -> ()
                            | true ->
                                do! client.SendAsync("ping", source.Token)
                                logger.LogDebug("Heartbeat sent")
                        with
                        | :? OperationCanceledException -> ()
                        | ex -> logger.LogError(ex, "Failed to send heartbeat")
                with :? OperationCanceledException ->
                    ()
            }

        heartbeatTask <- Some loop

    member this.Stop() =
        try
            cts |> Option.iter _.Cancel()
            heartbeatTask |> Option.iter (fun t -> t.Wait(TimeSpan.FromSeconds(5.0)) |> ignore)
            cts |> Option.iter _.Dispose()
            cts <- None
            heartbeatTask <- None
        with _ ->
            ()

    interface IDisposable with
        member this.Dispose() =
            this.Stop()
            timer.Dispose()
