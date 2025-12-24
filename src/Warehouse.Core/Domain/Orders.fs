namespace Warehouse.Core.Functional.Orders.Domain

open System
open Warehouse.Core.Functional.Markets.Domain

type OrderSide =
    | Buy = 0
    | Sell = 1

type OrderStatus =
    | Pending = 0
    | Placed = 1
    | PartiallyFilled = 2
    | Filled = 3
    | Cancelled = 4
    | Failed = 5

type Order =
    {
        Id: int64
        PipelineId: Nullable<int>
        MarketType: MarketType
        ExchangeOrderId: string
        Symbol: string
        Side: OrderSide
        Status: OrderStatus
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
