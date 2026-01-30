namespace Warehouse.Core.Markets.Services

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Markets.Abstractions
open Warehouse.Core.Repositories
open Warehouse.Core.Shared

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


module OrdersManager =
    open Errors

    let createOrder
        (repo: OrderRepository.T)
        (logger: ILogger)
        (request: CreateOrderRequest)
        (token: CancellationToken)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                let now = DateTime.UtcNow

                let order: Order =
                    {
                        Id = 0
                        PipelineId = request.PipelineId |> Option.toNullable
                        MarketType = request.MarketType
                        ExchangeOrderId = ""
                        Symbol = request.Symbol
                        Side = request.Side
                        Status = OrderStatus.Pending
                        Quantity = request.Quantity
                        Price = request.Price |> Option.toNullable
                        StopPrice = request.StopPrice |> Option.toNullable
                        TakeProfit = request.TakeProfit |> Option.toNullable
                        StopLoss = request.StopLoss |> Option.toNullable
                        Fee = Nullable()
                        PlacedAt = Nullable()
                        ExecutedAt = Nullable()
                        CancelledAt = Nullable()
                        CreatedAt = now
                        UpdatedAt = now
                    }

                let! inserted = repo.Insert order token
                let! order = repo.GetByPipeline inserted.PipelineId.Value (Some OrderStatus.Pending) token

                logger.LogInformation(
                    "Created order {OrderId} for {Symbol} {Side} {Quantity}",
                    inserted.Id,
                    inserted.Symbol,
                    inserted.Side,
                    inserted.Quantity
                )

                return Ok(order |> List.sortBy _.CreatedAt |> List.head)
            with ex ->
                logger.LogError(ex, "Failed to create order")
                return Error(Unexpected ex)
        }

    let executeOrder
        (repo: OrderRepository.T)
        (providers: OrderExecutor.T list)
        (logger: ILogger)
        (orderId: int)
        (token: CancellationToken)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                match! repo.GetById orderId token with
                | None -> return Error(NotFound $"Order {orderId}")
                | Some order when order.Status <> OrderStatus.Pending ->
                    return Error(ApiError($"Cannot execute order in status {order.Status}", None))

                | Some order ->
                    match OrderExecutor.tryFind order.MarketType providers with
                    | None -> return Error(NoProvider order.MarketType)
                    | Some provider ->
                        let! result = OrderExecutor.executeOrder order token provider

                        match result with
                        | Error err ->
                            let failedOrder = { order with Status = OrderStatus.Failed; UpdatedAt = DateTime.UtcNow }

                            do! repo.Update failedOrder token
                            return Error err
                        | Ok exchangeOrderId ->
                            let placedOrder =
                                { order with
                                    ExchangeOrderId = exchangeOrderId
                                    Status = OrderStatus.Placed
                                    PlacedAt = Nullable DateTime.UtcNow
                                    UpdatedAt = DateTime.UtcNow
                                }

                            do! repo.Update placedOrder token

                            logger.LogInformation(
                                "Placed order {OrderId} on {Market} with exchange ID {ExchangeOrderId}",
                                orderId,
                                order.MarketType,
                                exchangeOrderId
                            )

                            return Ok placedOrder
            with ex ->
                logger.LogError(ex, "Failed to execute order {OrderId}", orderId)
                return Error(Unexpected ex)
        }

    let updateOrderFields
        (repo: OrderRepository.T)
        (logger: ILogger)
        (orderId: int)
        (request: UpdateOrderRequest)
        (token: CancellationToken)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                match! repo.GetById orderId token with
                | None -> return Error(NotFound $"Order {orderId}")
                | Some order when order.Status <> OrderStatus.Placed && order.Status <> OrderStatus.PartiallyFilled ->
                    return Error(ApiError($"Cannot update order in status {order.Status}", None))
                | Some order ->
                    let updated =
                        { order with
                            Quantity = request.Quantity |> Option.defaultValue order.Quantity
                            Price =
                                request.Price |> Option.toNullable |> (fun x -> if x.HasValue then x else order.Price)
                            StopPrice =
                                request.StopPrice
                                |> Option.toNullable
                                |> (fun x -> if x.HasValue then x else order.StopPrice)
                            TakeProfit =
                                request.TakeProfit
                                |> Option.toNullable
                                |> (fun x -> if x.HasValue then x else order.TakeProfit)
                            StopLoss =
                                request.StopLoss
                                |> Option.toNullable
                                |> (fun x -> if x.HasValue then x else order.StopLoss)
                            UpdatedAt = DateTime.UtcNow
                        }

                    do! repo.Update updated token
                    logger.LogInformation("Updated order {OrderId}", orderId)
                    return Ok updated
            with ex ->
                logger.LogError(ex, "Failed to update order {OrderId}", orderId)
                return Error(Unexpected ex)
        }

    let cancelOrder
        (repo: OrderRepository.T)
        (logger: ILogger)
        (orderId: int)
        (reason: string option)
        (token: CancellationToken)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                match! repo.GetById orderId token with
                | None -> return Error(NotFound $"Order {orderId}")
                | Some order when order.Status = OrderStatus.Cancelled || order.Status = OrderStatus.Filled ->
                    return Error(ApiError($"Cannot cancel order in status {order.Status}", None))

                | Some order ->
                    let cancelled =
                        { order with
                            Status = OrderStatus.Cancelled
                            CancelledAt = Nullable DateTime.UtcNow
                            UpdatedAt = DateTime.UtcNow
                        }

                    do! repo.Update cancelled token
                    logger.LogInformation("Cancelled order {OrderId} with reason: {Reason}", orderId, reason)
                    return Ok cancelled
            with ex ->
                logger.LogError(ex, "Failed to cancel order {OrderId}", orderId)
                return Error(Unexpected ex)
        }

    let getOrder (repo: OrderRepository.T) (orderId: int) (token: CancellationToken) : Task<Order option> =
        repo.GetById orderId token

    let getOrderByExchangeId
        (repo: OrderRepository.T)
        (exchangeOrderId: string)
        (market: MarketType)
        (token: CancellationToken)
        : Task<Order option>
        =
        repo.GetByExchangeId exchangeOrderId market token

    let getOrders
        (repo: OrderRepository.T)
        (pipelineId: int)
        (status: OrderStatus option)
        (token: CancellationToken)
        : Task<Order list>
        =
        repo.GetByPipeline pipelineId status token

    let getOrderHistory
        (repo: OrderRepository.T)
        (skip: int)
        (take: int)
        (filter: OrderHistoryFilter option)
        (token: CancellationToken)
        : Task<Order list>
        =
        task {
            let! orders = repo.GetHistory skip take token

            match filter with
            | None -> return orders
            | Some filter ->
                return
                    orders
                    |> List.filter (fun x ->
                        (filter.PipelineId
                         |> Option.map (fun id -> x.PipelineId = Nullable id)
                         |> Option.defaultValue true)
                        && (filter.MarketType |> Option.map (fun y -> x.MarketType = y) |> Option.defaultValue true)
                        && (filter.Symbol |> Option.map (fun y -> x.Symbol = y) |> Option.defaultValue true)
                        && (filter.Status |> Option.map (fun y -> x.Status = y) |> Option.defaultValue true)
                        && (filter.Side |> Option.map (fun y -> x.Side = y) |> Option.defaultValue true)
                        && (filter.FromDate |> Option.map (fun y -> x.CreatedAt >= y) |> Option.defaultValue true)
                        && (filter.ToDate |> Option.map (fun y -> x.CreatedAt <= y) |> Option.defaultValue true)
                    )
        }

    let getTotalExposure
        (repo: OrderRepository.T)
        (market: MarketType option)
        (token: CancellationToken)
        : Task<decimal>
        =
        repo.GetTotalExposure market token

    type T =
        {
            createOrder: CreateOrderRequest -> CancellationToken -> Task<Result<Order, ServiceError>>
            executeOrder: int -> CancellationToken -> Task<Result<Order, ServiceError>>
            updateOrder: int -> UpdateOrderRequest -> CancellationToken -> Task<Result<Order, ServiceError>>
            cancelOrder: int -> string option -> CancellationToken -> Task<Result<Order, ServiceError>>
            getOrder: int -> CancellationToken -> Task<Order option>
            getOrders: int -> OrderStatus option -> CancellationToken -> Task<Order list>
            getOrderHistory: int -> int -> OrderHistoryFilter option -> CancellationToken -> Task<Order list>
            getOrderByExchangeId: string -> MarketType -> CancellationToken -> Task<Order option>
            getTotalExposure: MarketType option -> CancellationToken -> Task<decimal>
        }

    let create (repo: OrderRepository.T) (executors: OrderExecutor.T list) (logger: ILogger) : T =
        {
            createOrder = createOrder repo logger
            executeOrder = executeOrder repo executors logger
            updateOrder = updateOrderFields repo logger
            cancelOrder = cancelOrder repo logger
            getOrder = getOrder repo
            getOrders = getOrders repo
            getOrderHistory = getOrderHistory repo
            getOrderByExchangeId = getOrderByExchangeId repo
            getTotalExposure = getTotalExposure repo
        }
