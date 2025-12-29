namespace Warehouse.Core.Markets

open System
open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper.FSharp.PostgreSQL
open Microsoft.Extensions.Logging
open Warehouse.Core.Markets
open Warehouse.Core.Domain
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
    let private orders = table'<Order> "orders"

    let private findById (db: IDbConnection) (orderId: int64) : Task<Order option> =
        task {
            let! results =
                select {
                    for order in orders do
                        where (order.Id = orderId)
                        take 1
                }
                |> db.SelectAsync<Order>

            return results |> Seq.tryHead
        }

    let private findByExchangeId
        (db: IDbConnection)
        (exchangeOrderId: string)
        (market: MarketType)
        : Task<Order option>
        =
        task {
            let marketInt = int market

            let! results =
                select {
                    for order in orders do
                        where (order.ExchangeOrderId = exchangeOrderId && int order.MarketType = marketInt)
                        take 1
                }
                |> db.SelectAsync<Order>

            return results |> Seq.tryHead
        }

    let private insertOrder (db: IDbConnection) (order: Order) : Task<Order> =
        task {
            let! _ =
                insert {
                    into orders
                    value order
                }
                |> db.InsertAsync

            return order
        }

    let private updateOrder (db: IDbConnection) (order: Order) : Task<unit> =
        task {
            let! _ =
                update {
                    for o in orders do
                        setColumn o.Status order.Status
                        setColumn o.Quantity order.Quantity
                        setColumn o.Price order.Price
                        setColumn o.StopPrice order.StopPrice
                        setColumn o.TakeProfit order.TakeProfit
                        setColumn o.StopLoss order.StopLoss
                        setColumn o.ExchangeOrderId order.ExchangeOrderId
                        setColumn o.PlacedAt order.PlacedAt
                        setColumn o.ExecutedAt order.ExecutedAt
                        setColumn o.CancelledAt order.CancelledAt
                        setColumn o.Fee order.Fee
                        setColumn o.UpdatedAt DateTime.UtcNow
                        where (o.Id = order.Id)
                }
                |> db.UpdateAsync

            return ()
        }

    let createOrder
        (db: IDbConnection)
        (logger: ILogger)
        (request: CreateOrderRequest)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                let now = DateTime.UtcNow

                let order: Order =
                    {
                        Id = 0L
                        PipelineId = request.PipelineId |> Option.toNullable
                        MarketType = request.MarketType
                        ExchangeOrderId = null
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

                let! inserted = insertOrder db order

                logger.LogInformation(
                    "Created order {OrderId} for {Symbol} {Side} {Quantity}",
                    inserted.Id,
                    inserted.Symbol,
                    inserted.Side,
                    inserted.Quantity
                )

                return Ok inserted
            with ex ->
                logger.LogError(ex, "Failed to create order")
                return Error(Unexpected ex)
        }

    let executeOrder
        (db: IDbConnection)
        (providers: OrderService.T list)
        (logger: ILogger)
        (orderId: int64)
        (_: CancellationToken)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                match! findById db orderId with
                | None -> return Error(NotFound $"Order {orderId}")
                | Some order when order.Status <> OrderStatus.Pending ->
                    return Error(ApiError($"Cannot execute order in status {order.Status}", None))

                | Some order ->
                    match OrderService.tryFind order.MarketType providers with
                    | None -> return Error(NoProvider order.MarketType)
                    | Some provider ->
                        let! result = OrderService.executeOrder order CancellationToken.None provider

                        match result with
                        | Error err ->
                            let failedOrder = { order with Status = OrderStatus.Failed; UpdatedAt = DateTime.UtcNow }
                            do! updateOrder db failedOrder
                            return Error err
                        | Ok exchangeOrderId ->
                            let placedOrder =
                                { order with
                                    ExchangeOrderId = exchangeOrderId
                                    Status = OrderStatus.Placed
                                    PlacedAt = Nullable DateTime.UtcNow
                                    UpdatedAt = DateTime.UtcNow
                                }

                            do! updateOrder db placedOrder

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
        (db: IDbConnection)
        (logger: ILogger)
        (orderId: int64)
        (request: UpdateOrderRequest)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                match! findById db orderId with
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

                    do! updateOrder db updated
                    logger.LogInformation("Updated order {OrderId}", orderId)
                    return Ok updated
            with ex ->
                logger.LogError(ex, "Failed to update order {OrderId}", orderId)
                return Error(Unexpected ex)
        }

    let cancelOrder
        (db: IDbConnection)
        (logger: ILogger)
        (orderId: int64)
        (reason: string option)
        : Task<Result<Order, ServiceError>>
        =
        task {
            try
                match! findById db orderId with
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

                    do! updateOrder db cancelled
                    logger.LogInformation("Cancelled order {OrderId} with reason: {Reason}", orderId, reason)
                    return Ok cancelled
            with ex ->
                logger.LogError(ex, "Failed to cancel order {OrderId}", orderId)
                return Error(Unexpected ex)
        }

    let getOrder (db: IDbConnection) (orderId: int64) : Task<Order option> = findById db orderId

    let getOrderByExchangeId (db: IDbConnection) (exchangeOrderId: string) (market: MarketType) : Task<Order option> =
        findByExchangeId db exchangeOrderId market

    let getOrders (db: IDbConnection) (pipelineId: int) (status: OrderStatus option) : Task<Order list> =
        task {
            let! results =
                match status with
                | Some status ->
                    select {
                        for order in orders do
                            where (order.PipelineId = Nullable pipelineId && order.Status = status)
                            orderByDescending order.CreatedAt
                    }
                    |> db.SelectAsync<Order>
                | None ->
                    select {
                        for order in orders do
                            where (order.PipelineId = Nullable pipelineId)
                            orderByDescending order.CreatedAt
                    }
                    |> db.SelectAsync<Order>

            return results |> Seq.toList
        }

    let getOrderHistory
        (db: IDbConnection)
        (skip: int)
        (take: int)
        (filter: OrderHistoryFilter option)
        : Task<Order list>
        =
        task {
            let! results =
                match filter with
                | None ->
                    let skipCount = skip
                    let takeCount = take

                    select {
                        for order in orders do
                            orderByDescending order.CreatedAt
                            skip skipCount
                            take takeCount
                    }
                    |> db.SelectAsync<Order>

                | Some filter ->
                    select {
                        for o in orders do
                            orderByDescending o.CreatedAt
                    }
                    |> db.SelectAsync<Order>

            match filter with
            | None -> return results |> Seq.toList
            | Some filter ->
                return
                    results
                    |> Seq.filter (fun x ->
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
                    |> Seq.skip skip
                    |> Seq.truncate take
                    |> Seq.toList
        }

    let getTotalExposure (db: IDbConnection) (market: MarketType option) : Task<decimal> =
        task {
            let! results =
                select {
                    for order in orders do
                        where (
                            order.Status = OrderStatus.Placed
                            || order.Status = OrderStatus.PartiallyFilled
                            || order.Status = OrderStatus.Filled
                        )
                }
                |> db.SelectAsync<Order>

            return
                results
                |> Seq.filter (fun x -> market |> Option.map (fun m -> x.MarketType = m) |> Option.defaultValue true)
                |> Seq.sumBy (fun x ->
                    let price = if x.Price.HasValue then x.Price.Value else 0m
                    x.Quantity * price
                )
        }

    type T =
        {
            createOrder: CreateOrderRequest -> Task<Result<Order, ServiceError>>
            executeOrder: int64 -> CancellationToken -> Task<Result<Order, ServiceError>>
            updateOrder: int64 -> UpdateOrderRequest -> Task<Result<Order, ServiceError>>
            cancelOrder: int64 -> string option -> Task<Result<Order, ServiceError>>
            getOrder: int64 -> Task<Order option>
            getOrders: int -> OrderStatus option -> Task<Order list>
            getOrderHistory: int -> int -> OrderHistoryFilter option -> Task<Order list>
            getOrderByExchangeId: string -> MarketType -> Task<Order option>
            getTotalExposure: MarketType option -> Task<decimal>
        }

    let create (db: IDbConnection) (providers: OrderService.T list) (logger: ILogger) : T =
        {
            createOrder = createOrder db logger
            executeOrder = executeOrder db providers logger
            updateOrder = updateOrderFields db logger
            cancelOrder = cancelOrder db logger
            getOrder = getOrder db
            getOrders = getOrders db
            getOrderHistory = getOrderHistory db
            getOrderByExchangeId = getOrderByExchangeId db
            getTotalExposure = getTotalExposure db
        }
