namespace Warehouse.Core.Functional.Orders.Domain

open System
open Warehouse.Core.Functional.Shared.Domain
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

[<AllowNullLiteral>]
type Order() =
    inherit AuditEntity()

    member val Id: int64 = 0L with get, set
    member val PipelineId: Nullable<int> = Nullable() with get, set
    member val MarketType: MarketType = MarketType.Okx with get, set
    member val ExchangeOrderId: string = String.Empty with get, set
    member val Symbol: string = String.Empty with get, set
    member val Side: OrderSide = OrderSide.Buy with get, set
    member val Status: OrderStatus = OrderStatus.Pending with get, set
    member val Quantity: decimal = 0m with get, set
    member val Price: Nullable<decimal> = Nullable() with get, set
    member val StopPrice: Nullable<decimal> = Nullable() with get, set
    member val Fee: Nullable<decimal> = Nullable() with get, set
    member val PlacedAt: Nullable<DateTime> = Nullable() with get, set
    member val ExecutedAt: Nullable<DateTime> = Nullable() with get, set
    member val CancelledAt: Nullable<DateTime> = Nullable() with get, set
    member val TakeProfit: Nullable<decimal> = Nullable() with get, set
    member val StopLoss: Nullable<decimal> = Nullable() with get, set
