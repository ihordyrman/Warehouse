namespace Warehouse.Core.Infrastructure

open System
open Warehouse.Core.Domain
open System.Collections.Generic
open System.Text.Json

module Entities =

    [<CLIMutable>]
    type MarketEntity = { Id: int; Type: int; CreatedAt: DateTime; UpdatedAt: DateTime }

    [<CLIMutable>]
    type MarketCredentialsEntity =
        {
            Id: int
            MarketId: int
            ApiKey: string
            SecretKey: string
            Passphrase: string
            IsSandbox: bool
            CreatedAt: DateTime
            UpdatedAt: DateTime
        }

    [<CLIMutable>]
    type PipelineConfigurationEntity =
        {
            Id: int
            Name: string
            Symbol: string
            MarketType: int
            Enabled: bool
            ExecutionInterval: int64
            LastExecutedAt: Nullable<DateTime>
            Status: int
            Tags: string
            CreatedAt: DateTime
            UpdatedAt: DateTime
        }

    [<CLIMutable>]
    type PipelineStepEntity =
        {
            Id: int
            PipelineDetailsId: int
            StepTypeKey: string
            Name: string
            Order: int
            IsEnabled: bool
            Parameters: string
            CreatedAt: DateTime
            UpdatedAt: DateTime
        }

    [<CLIMutable>]
    type PositionEntity =
        {
            Id: int
            PipelineId: Nullable<int>
            Symbol: string
            Status: int
            EntryPrice: Nullable<decimal>
            Quantity: Nullable<decimal>
            BuyOrderId: string
            SellOrderId: string
            ExitPrice: Nullable<decimal>
            ClosedAt: Nullable<DateTime>
            CreatedAt: DateTime
            UpdatedAt: DateTime
        }

    [<CLIMutable>]
    type CandlestickEntity =
        {
            Id: int
            Symbol: string
            MarketType: int
            Timestamp: DateTime
            Timeframe: string
            Open: decimal
            High: decimal
            Low: decimal
            Close: decimal
            Volume: decimal
            VolumeQuote: decimal
            IsCompleted: bool
        }

    [<CLIMutable>]
    type OrderEntity =
        {
            Id: int64
            PipelineId: Nullable<int>
            MarketType: int
            ExchangeOrderId: string
            Symbol: string
            Side: int
            Status: int
            Quantity: decimal
            Price: Nullable<decimal>
            StopPrice: Nullable<decimal>
            Fee: Nullable<decimal>
            PlacedAt: Nullable<DateTime>
            ExecutedAt: Nullable<DateTime>
            CancelledAt: Nullable<DateTime>
            TakeProfit: Nullable<decimal>
            StopLoss: Nullable<decimal>
            CreatedAt: DateTime
            UpdatedAt: DateTime
        }

module EntityMapping =
    open Entities

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

    let toMarket (entity: MarketEntity) (credentials: MarketCredentials option) : Market =
        {
            Id = entity.Id
            Type = enum<MarketType> entity.Type
            Credentials = credentials |> Option.defaultValue Unchecked.defaultof<MarketCredentials>
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    let toMarketCredentials (entity: MarketCredentialsEntity) (market: Market option) : MarketCredentials =
        {
            Id = entity.Id
            MarketId = entity.MarketId
            Market = market |> Option.defaultValue Unchecked.defaultof<Market>
            ApiKey = entity.ApiKey
            SecretKey = entity.SecretKey
            Passphrase = entity.Passphrase
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

    let toPipeline (entity: PipelineConfigurationEntity) : Pipeline =
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

    let toPosition (entity: PositionEntity) (pipeline: Pipeline option) : Position =
        {
            Id = entity.Id
            PipelineId = entity.PipelineId |> Option.ofNullable |> Option.defaultValue 0
            Pipeline = pipeline |> Option.defaultValue Unchecked.defaultof<Pipeline>
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
