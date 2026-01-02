namespace Warehouse.Core.Infrastructure

open System

module Entities =

    type MarketEntity = { Id: int; Type: int; CreatedAt: DateTime; UpdatedAt: DateTime }

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
