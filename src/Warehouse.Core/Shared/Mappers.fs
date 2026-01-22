namespace Warehouse.Core.Infrastructure

open System
open System.Collections.Generic
open System.Text.Json
open Warehouse.Core.Domain

module internal Mappers =
    open Entities

    /// Entities -> Domain mappers

    let private jsonOptions =
        let opts = JsonSerializerOptions()
        opts.PropertyNameCaseInsensitive <- true
        opts

    let private parseJsonList (json: string) : string list =
        if String.IsNullOrWhiteSpace(json) then
            []
        else
            try
                JsonSerializer.Deserialize<string list>(json, jsonOptions)
            with _ ->
                []

    let private parseJsonDict (json: string) : Dictionary<string, string> =
        if String.IsNullOrWhiteSpace(json) then
            Dictionary<string, string>()
        else
            try
                JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions)
            with _ ->
                Dictionary<string, string>()

    let toMarket (entity: MarketEntity) : Market =
        {
            Id = entity.Id
            Type = enum<MarketType> entity.Type
            ApiKey = entity.ApiKey
            SecretKey = entity.SecretKey
            Passphrase = if String.IsNullOrWhiteSpace(entity.Passphrase) then None else Some entity.Passphrase
            IsSandbox = entity.IsSandbox
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    let toPipelineStep (entity: PipelineStepEntity) (pipeline: Pipeline option) : PipelineStep =
        {
            Id = entity.Id
            PipelineDetailsId = entity.PipelineDetailsId
            Pipeline = pipeline |> Option.defaultValue Unchecked.defaultof<Pipeline>
            StepTypeKey = entity.StepTypeKey
            Name = entity.Name
            Order = entity.Order
            IsEnabled = entity.IsEnabled
            Parameters = parseJsonDict entity.Parameters
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    let toPipeline (entity: PipelineEntity) : Pipeline =
        {
            Id = entity.Id
            Name = entity.Name
            Symbol = entity.Symbol
            MarketType = enum<MarketType> entity.MarketType
            Enabled = entity.Enabled
            ExecutionInterval = TimeSpan.FromTicks(entity.ExecutionInterval)
            LastExecutedAt = entity.LastExecutedAt
            Status = enum<PipelineStatus> entity.Status
            Steps = []
            Tags = parseJsonList entity.Tags
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    let toPosition (entity: PositionEntity) : Position =
        {
            Id = entity.Id
            PipelineId = entity.PipelineId |> Option.ofNullable |> Option.defaultValue 0
            Symbol = entity.Symbol
            Status = enum<PositionStatus> entity.Status
            EntryPrice = entity.EntryPrice |> Option.ofNullable |> Option.defaultValue 0m
            Quantity = entity.Quantity |> Option.ofNullable |> Option.defaultValue 0m
            BuyOrderId = entity.BuyOrderId
            SellOrderId = entity.SellOrderId
            ExitPrice = entity.ExitPrice
            ClosedAt = entity.ClosedAt
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    let toCandlestick (entity: CandlestickEntity) : Candlestick =
        {
            Id = entity.Id.GetHashCode() |> int64
            Symbol = entity.Symbol
            MarketType = entity.MarketType
            Timestamp = entity.Timestamp
            Timeframe = entity.Timeframe
            Open = entity.Open
            High = entity.High
            Low = entity.Low
            Close = entity.Close
            Volume = entity.Volume
            VolumeQuote = entity.VolumeQuote
            IsCompleted = entity.IsCompleted
        }

    let toOrder (entity: OrderEntity) : Order =
        {
            Id = entity.Id
            PipelineId = entity.PipelineId
            MarketType = enum<MarketType> entity.MarketType
            ExchangeOrderId = entity.ExchangeOrderId
            Symbol = entity.Symbol
            Side = enum<OrderSide> entity.Side
            Status = enum<OrderStatus> entity.Status
            Quantity = entity.Quantity
            Price = entity.Price
            StopPrice = entity.StopPrice
            Fee = entity.Fee
            PlacedAt = entity.PlacedAt
            ExecutedAt = entity.ExecutedAt
            CancelledAt = entity.CancelledAt
            TakeProfit = entity.TakeProfit
            StopLoss = entity.StopLoss
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    /// Domain -> Entities mappers

    let fromPipeline (pipeline: Pipeline) : PipelineEntity =
        {
            Id = pipeline.Id
            Name = pipeline.Name
            Symbol = pipeline.Symbol
            MarketType = int pipeline.MarketType
            Enabled = pipeline.Enabled
            ExecutionInterval = pipeline.ExecutionInterval.Ticks
            LastExecutedAt = pipeline.LastExecutedAt
            Status = int pipeline.Status
            Tags = JsonSerializer.Serialize(pipeline.Tags, jsonOptions)
            CreatedAt = pipeline.CreatedAt
            UpdatedAt = pipeline.UpdatedAt
        }

    let fromPipelineStep (step: PipelineStep) : PipelineStepEntity =
        {
            Id = step.Id
            PipelineDetailsId = step.PipelineDetailsId
            StepTypeKey = step.StepTypeKey
            Name = step.Name
            Order = step.Order
            IsEnabled = step.IsEnabled
            Parameters = JsonSerializer.Serialize(step.Parameters, jsonOptions)
            CreatedAt = step.CreatedAt
            UpdatedAt = step.UpdatedAt
        }

    let fromPosition (position: Position) : PositionEntity =
        {
            Id = position.Id
            PipelineId = Nullable(position.PipelineId)
            Symbol = position.Symbol
            Status = int position.Status
            EntryPrice = Nullable(position.EntryPrice)
            Quantity = Nullable(position.Quantity)
            BuyOrderId = position.BuyOrderId
            SellOrderId = position.SellOrderId
            ExitPrice = position.ExitPrice
            ClosedAt = position.ClosedAt
            CreatedAt = position.CreatedAt
            UpdatedAt = position.UpdatedAt
        }

    let fromOrder (order: Order) : OrderEntity =
        {
            Id = order.Id
            PipelineId = order.PipelineId
            MarketType = int order.MarketType
            ExchangeOrderId = order.ExchangeOrderId
            Symbol = order.Symbol
            Side = int order.Side
            Status = int order.Status
            Quantity = order.Quantity
            Price = order.Price
            StopPrice = order.StopPrice
            Fee = order.Fee
            PlacedAt = order.PlacedAt
            ExecutedAt = order.ExecutedAt
            CancelledAt = order.CancelledAt
            TakeProfit = order.TakeProfit
            StopLoss = order.StopLoss
            CreatedAt = order.CreatedAt
            UpdatedAt = order.UpdatedAt
        }

    let fromMarket (market: Market) : MarketEntity =
        {
            Id = market.Id
            Type = int market.Type
            ApiKey = market.ApiKey
            Passphrase = market.Passphrase |> Option.defaultValue String.Empty
            SecretKey = market.SecretKey
            IsSandbox = market.IsSandbox
            CreatedAt = market.CreatedAt
            UpdatedAt = market.UpdatedAt
        }

    let fromCandlestick (candlestick: Candlestick) : CandlestickEntity =
        {
            Id = candlestick.Id.GetHashCode()
            Symbol = candlestick.Symbol
            MarketType = candlestick.MarketType
            Timestamp = candlestick.Timestamp
            Timeframe = candlestick.Timeframe
            Open = candlestick.Open
            High = candlestick.High
            Low = candlestick.Low
            Close = candlestick.Close
            Volume = candlestick.Volume
            VolumeQuote = candlestick.VolumeQuote
            IsCompleted = candlestick.IsCompleted
        }
