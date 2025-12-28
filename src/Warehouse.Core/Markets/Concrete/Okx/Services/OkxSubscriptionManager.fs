namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Warehouse.Core.Markets.Domain

type SubscriptionInfo = { Channel: string; Symbol: string; SubscribedAt: DateTime }

type OkxSubscriptionManager(connectionManager: OkxConnectionManager, logger: ILogger<OkxSubscriptionManager>) =
    let subscriptionLock = new SemaphoreSlim(1, 1)
    let subscriptions = ConcurrentDictionary<string, SubscriptionInfo>(StringComparer.OrdinalIgnoreCase)

    let onConnectionStateChanged (_: obj) (state: ConnectionState) =
        if state = ConnectionState.Connected && not subscriptions.IsEmpty then
            logger.LogInformation("Resubscribing to {Count} channels after reconnection", subscriptions.Count)

            task {
                for kvp in subscriptions do
                    let sub = kvp.Value

                    try
                        let request =
                            {| op = "subscribe"; args = [| {| channel = sub.Channel; instId = sub.Symbol |} |] |}

                        do! connectionManager.SendAsync(request)
                        logger.LogDebug("Resubscribed to {Channel}:{Symbol}", sub.Channel, sub.Symbol)
                    with ex ->
                        logger.LogError(ex, "Failed to resubscribe to {Channel}:{Symbol}", sub.Channel, sub.Symbol)
            }
            |> ignore

    do connectionManager.StateChanged.AddHandler(EventHandler<ConnectionState>(onConnectionStateChanged))

    member this.SubscribeAsync(channel: string, symbol: string, ?cancellationToken: CancellationToken) =
        task {
            let ct = defaultArg cancellationToken CancellationToken.None
            let key = $"{channel}:{symbol}"
            do! subscriptionLock.WaitAsync(ct)

            try
                try
                    if subscriptions.ContainsKey(key) then
                        logger.LogDebug("Already subscribed to {Key}", key)
                        return true
                    else
                        let request = {| op = "subscribe"; args = [| {| channel = channel; instId = symbol |} |] |}
                        do! connectionManager.SendAsync(request, ct)

                        subscriptions[key] <- { Channel = channel; Symbol = symbol; SubscribedAt = DateTime.UtcNow }
                        logger.LogInformation("Subscribed to {Channel} for {Symbol}", channel, symbol)
                        return true
                with ex ->
                    logger.LogError(ex, "Failed to subscribe to {Key}", key)
                    return false
            finally
                subscriptionLock.Release() |> ignore
        }

    member this.UnsubscribeAsync(channel: string, symbol: string, ?cancellationToken: CancellationToken) =
        task {
            let ct = defaultArg cancellationToken CancellationToken.None
            let key = $"{channel}:{symbol}"
            do! subscriptionLock.WaitAsync(ct)

            try
                try
                    match subscriptions.TryRemove(key) with
                    | false, _ ->
                        logger.LogDebug("Not subscribed to {Key}", key)
                        return false
                    | true, _ ->
                        let request = {| op = "unsubscribe"; args = [| {| channel = channel; instId = symbol |} |] |}
                        do! connectionManager.SendAsync(request, ct)
                        logger.LogInformation("Unsubscribed from {Channel} for {Symbol}", channel, symbol)
                        return true
                with ex ->
                    logger.LogError(ex, "Failed to unsubscribe from {Key}", key)
                    return false
            finally
                subscriptionLock.Release() |> ignore
        }

    member this.UnsubscribeAllAsync(?cancellationToken: CancellationToken) =
        task {
            let ct = defaultArg cancellationToken CancellationToken.None

            let tasks =
                subscriptions |> Seq.map (fun kvp -> this.UnsubscribeAsync(kvp.Value.Channel, kvp.Value.Symbol, ct))

            do! Task.WhenAll(tasks) :> Task
        }
