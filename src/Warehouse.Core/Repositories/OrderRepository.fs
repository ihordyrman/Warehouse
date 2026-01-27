namespace Warehouse.Core.Repositories

open System
open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain

module OrderRepository =

    type T =
        {
            GetById: int64 -> CancellationToken -> Task<Order option>
            GetByExchangeId: string -> MarketType -> CancellationToken -> Task<Order option>
            GetByPipeline: int -> OrderStatus option -> CancellationToken -> Task<Order list>
            GetHistory: int -> int -> CancellationToken -> Task<Order list>
            GetTotalExposure: MarketType option -> CancellationToken -> Task<decimal>
            Insert: Order -> CancellationToken -> Task<Order>
            Update: Order -> CancellationToken -> Task<unit>
        }

    let private getById
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (orderId: int64)
        (token: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! order =
                    db.QueryFirstOrDefaultAsync<Order>(
                        CommandDefinition(
                            "SELECT * FROM orders WHERE id = @Id",
                            {| Id = orderId |},
                            cancellationToken = token
                        )
                    )

                match box order with
                | null -> return None
                | _ -> return Some order
            with ex ->
                logger.LogError(ex, "Failed to get order {OrderId}", orderId)
                return None
        }

    let private getByExchangeId
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (exchangeOrderId: string)
        (market: MarketType)
        (token: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! order =
                    db.QueryFirstOrDefaultAsync<Order>(
                        CommandDefinition(
                            "SELECT * FROM orders WHERE exchange_order_id = @ExchangeOrderId AND market_type = @MarketType",
                            {|
                                ExchangeOrderId = exchangeOrderId
                                MarketType = int market
                            |},
                            cancellationToken = token
                        )
                    )

                match box order with
                | null -> return None
                | _ -> return Some order
            with ex ->
                logger.LogError(ex, "Failed to get order by exchange id {ExchangeOrderId}", exchangeOrderId)
                return None
        }

    let private getByPipeline
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (pipelineId: int)
        (status: OrderStatus option)
        (token: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! orders =
                    match status with
                    | Some s ->
                        db.QueryAsync<Order>(
                            CommandDefinition(
                                "SELECT * FROM orders WHERE pipeline_id = @PipelineId AND status = @Status ORDER BY created_at DESC",
                                {|
                                    PipelineId = pipelineId
                                    Status = int s
                                |},
                                cancellationToken = token
                            )
                        )
                    | None ->
                        db.QueryAsync<Order>(
                            CommandDefinition(
                                "SELECT * FROM orders WHERE pipeline_id = @PipelineId ORDER BY created_at DESC",
                                {| PipelineId = pipelineId |},
                                cancellationToken = token
                            )
                        )

                return orders |> Seq.toList
            with ex ->
                logger.LogError(ex, "Failed to get orders for pipeline {PipelineId}", pipelineId)
                return []
        }

    let private getHistory
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (skip: int)
        (take: int)
        (token: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! orders =
                    db.QueryAsync<Order>(
                        CommandDefinition(
                            "SELECT * FROM orders ORDER BY created_at DESC OFFSET @Skip LIMIT @Take",
                            {| Skip = skip; Take = take |},
                            cancellationToken = token
                        )
                    )

                return orders |> Seq.toList
            with ex ->
                logger.LogError(ex, "Failed to get order history")
                return []
        }

    let private getTotalExposure
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (market: MarketType option)
        (token: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let sql, parameters =
                    match market with
                    | Some m ->
                        """SELECT COALESCE(SUM(quantity * COALESCE(price, 0)), 0)
                           FROM orders
                           WHERE status IN (@Placed, @PartiallyFilled, @Filled)
                           AND market_type = @MarketType""",
                        {|
                            Placed = int OrderStatus.Placed
                            PartiallyFilled = int OrderStatus.PartiallyFilled
                            Filled = int OrderStatus.Filled
                            MarketType = int m
                        |}
                        :> obj
                    | None ->
                        """SELECT COALESCE(SUM(quantity * COALESCE(price, 0)), 0)
                           FROM orders
                           WHERE status IN (@Placed, @PartiallyFilled, @Filled)""",
                        {|
                            Placed = int OrderStatus.Placed
                            PartiallyFilled = int OrderStatus.PartiallyFilled
                            Filled = int OrderStatus.Filled
                        |}
                        :> obj

                let! result =
                    db.QuerySingleAsync<decimal>(CommandDefinition(sql, parameters, cancellationToken = token))

                return result
            with ex ->
                logger.LogError(ex, "Failed to get total exposure")
                return 0m
        }

    let private insert
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (order: Order)
        (token: CancellationToken)
        =
        task {
            use scope = scopeFactory.CreateScope()
            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

            let! id =
                db.QuerySingleAsync<int64>(
                    CommandDefinition(
                        """INSERT INTO orders
                           (pipeline_id, market_type, exchange_order_id, symbol, side, status, quantity, price, stop_price, fee, placed_at, executed_at, cancelled_at, take_profit, stop_loss, created_at, updated_at)
                           VALUES (@PipelineId, @MarketType, @ExchangeOrderId, @Symbol, @Side, @Status, @Quantity, @Price, @StopPrice, @Fee, @PlacedAt, @ExecutedAt, @CancelledAt, @TakeProfit, @StopLoss, @CreatedAt, @UpdatedAt)
                           RETURNING id""",
                        {|
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
                        |},
                        cancellationToken = token
                    )
                )

            logger.LogDebug("Inserted order {OrderId}", id)
            return { order with Id = id }
        }

    let private update
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (order: Order)
        (token: CancellationToken)
        =
        task {
            use scope = scopeFactory.CreateScope()
            use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

            let! _ =
                db.ExecuteAsync(
                    CommandDefinition(
                        """UPDATE orders
                           SET status = @Status, quantity = @Quantity, price = @Price, stop_price = @StopPrice,
                               take_profit = @TakeProfit, stop_loss = @StopLoss, exchange_order_id = @ExchangeOrderId,
                               placed_at = @PlacedAt, executed_at = @ExecutedAt, cancelled_at = @CancelledAt,
                               fee = @Fee, updated_at = @UpdatedAt
                           WHERE id = @Id""",
                        {|
                            Id = order.Id
                            Status = int order.Status
                            Quantity = order.Quantity
                            Price = order.Price
                            StopPrice = order.StopPrice
                            TakeProfit = order.TakeProfit
                            StopLoss = order.StopLoss
                            ExchangeOrderId = order.ExchangeOrderId
                            PlacedAt = order.PlacedAt
                            ExecutedAt = order.ExecutedAt
                            CancelledAt = order.CancelledAt
                            Fee = order.Fee
                            UpdatedAt = order.UpdatedAt
                        |},
                        cancellationToken = token
                    )
                )

            logger.LogDebug("Updated order {OrderId}", order.Id)
        }

    let create (scopeFactory: IServiceScopeFactory) : T =
        let loggerFactory =
            scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILoggerFactory>()

        let logger = loggerFactory.CreateLogger("OrderRepository")

        {
            GetById = getById scopeFactory logger
            GetByExchangeId = getByExchangeId scopeFactory logger
            GetByPipeline = getByPipeline scopeFactory logger
            GetHistory = getHistory scopeFactory logger
            GetTotalExposure = getTotalExposure scopeFactory logger
            Insert = insert scopeFactory logger
            Update = update scopeFactory logger
        }
