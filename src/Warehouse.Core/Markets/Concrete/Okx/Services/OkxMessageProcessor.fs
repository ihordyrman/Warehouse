namespace Warehouse.Core.Markets.Concrete.Okx.Services

open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.Extensions.Logging
open Warehouse.Core.Infrastructure.WebSockets
open Warehouse.Core.Markets.Contracts
open Warehouse.Core.Markets.Domain
open Warehouse.Core.Markets.Okx

type OkxMessageProcessor(logger: ILogger<OkxMessageProcessor>, marketDataCache: IMarketDataCache) =
    let serializerOptions = JsonSerializerOptions()

    let processMarketData (message: OkxSocketResponse) =
        match message.Arguments, message.Data with
        | Some args, Some dataArray when dataArray.Length > 0 ->
            let symbol = args.InstrumentId |> Option.defaultValue ""
            let data = dataArray[0]

            match data.Asks, data.Bids with
            | Some asks, Some bids ->
                marketDataCache.Update { Symbol = symbol; Source = MarketType.Okx; Asks = asks; Bids = bids }
            | _ -> logger.LogWarning("Invalid market data for {Symbol}: missing asks or bids", symbol)
        | _ -> ()

    let routeMessage (message: OkxSocketResponse) =
        match message.Event with
        | Some OkxEvent.Subscribe ->
            logger.LogInformation(
                "Subscription confirmed: {Channel}:{Symbol}",
                (message.Arguments |> Option.bind _.Channel |> Option.defaultValue ""),
                (message.Arguments |> Option.bind _.InstrumentId |> Option.defaultValue "")
            )
        | Some OkxEvent.Error ->
            logger.LogError("OKX Error - Code: {Code}, Message: {Message}", message.Code, message.Message)
        | _ ->
            if message.Data.IsSome && message.Arguments.IsSome then
                processMarketData message

    member this.ProcessMessage(message: WebSocketMessage) =
        match message.Text with
        | None -> ()
        | Some "pong" -> logger.LogTrace("Heartbeat pong received")
        | Some text ->
            try
                serializerOptions.Converters.Add(JsonStringEnumConverter<OkxAction>())
                let okxMessage = JsonSerializer.Deserialize<OkxSocketResponse>(text, serializerOptions)

                if isNull (box okxMessage) then
                    logger.LogWarning("Failed to deserialize message: {Message}", text)
                else
                    routeMessage okxMessage
            with ex ->
                logger.LogError(ex, "Error processing message: {Message}", text)
