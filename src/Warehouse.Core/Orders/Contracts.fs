namespace Warehouse.Core.Orders.Contracts

open System
open System.Threading
open System.Threading.Tasks
open Warehouse.Core.Markets.Domain
open Warehouse.Core.Orders.Domain
open Warehouse.Core.Shared.Errors

[<CLIMutable>]
type CreateOrderRequest =
    {
        PipelineId: int option
        MarketType: MarketType
        Symbol: string
        Side: OrderSide
        Quantity: decimal
        Price: decimal option
        StopPrice: decimal option
        TakeProfit: decimal option
        StopLoss: decimal option
        ExpireTime: DateTime option
    }

[<CLIMutable>]
type OrderHistoryFilter =
    {
        PipelineId: int option
        MarketType: MarketType option
        Symbol: string option
        Status: OrderStatus option
        Side: OrderSide option
        FromDate: DateTime option
        ToDate: DateTime option
    }

[<CLIMutable>]
type UpdateOrderRequest =
    {
        Quantity: decimal option
        Price: decimal option
        StopPrice: decimal option
        TakeProfit: decimal option
        StopLoss: decimal option
    }

type IMarketOrderProvider =
    abstract member MarketType: MarketType with get

    abstract member ExecuteOrderAsync:
        order: Order * cancellationToken: CancellationToken -> Task<Result<string, ServiceError>>

type IOrderManager =
    abstract member CreateOrderAsync:
        request: CreateOrderRequest * cancellationToken: CancellationToken -> Task<Result<Order, ServiceError>>

    abstract member ExecuteOrderAsync:
        orderId: int64 * cancellationToken: CancellationToken -> Task<Result<Order, ServiceError>>

    abstract member UpdateOrderAsync:
        orderId: int64 * request: UpdateOrderRequest * cancellationToken: CancellationToken ->
            Task<Result<Order, ServiceError>>

    abstract member CancelOrderAsync:
        orderId: int64 * reason: string option * cancellationToken: CancellationToken -> Task<Result<Order,
                                                                                                  ServiceError>>

    abstract member GetOrderAsync: orderId: int64 * cancellationToken: CancellationToken -> Task<Order option>

    abstract member GetOrdersAsync:
        workerId: int * status: OrderStatus option * cancellationToken: CancellationToken -> Task<Order list>

    abstract member GetOrderHistoryAsync:
        skip: int * take: int * filter: OrderHistoryFilter option * cancellationToken: CancellationToken ->
            Task<Order list>
