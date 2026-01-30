namespace Warehouse.Core.Repositories

open System
open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Shared.Errors

module PositionRepository =

    type CreatePositionRequest =
        {
            PipelineId: int
            Symbol: string
            EntryPrice: decimal
            Quantity: decimal
            BuyOrderId: int
            Status: PositionStatus
        }

    type UpdatePositionRequest =
        {
            Id: int
            ExitPrice: decimal option
            SellOrderId: int option
            Status: PositionStatus
            ClosedAt: DateTime option
        }

    type T =
        {
            GetOpen: int -> CancellationToken -> Task<Result<Option<Position>, ServiceError>>
            Create: CreatePositionRequest -> CancellationToken -> Task<Result<Position, ServiceError>>
            Update: UpdatePositionRequest -> CancellationToken -> Task<Result<Position, ServiceError>>
        }

    let private getOpen
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (pipelineId: int)
        (cancellation: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! positions =
                    db.QueryAsync<Position>(
                        CommandDefinition(
                            "SELECT *
                              FROM positions
                              WHERE pipeline_id = @PipelineId AND status = @Status
                              LIMIT 1",
                            {| PipelineId = pipelineId; Status = int PositionStatus.Open |},
                            cancellationToken = cancellation
                        )
                    )

                match positions |> Seq.tryHead with
                | None ->
                    logger.LogDebug("No open position found for pipeline {PipelineId}", pipelineId)
                    return Ok None
                | Some position ->
                    logger.LogDebug("Retrieved open position for pipeline {PipelineId}", pipelineId)
                    return Ok(Some(position))
            with ex ->
                logger.LogError(ex, "Failed to get open position for pipeline {PipelineId}", pipelineId)
                return Result.Error(Unexpected ex)
        }

    let private createPosition
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (request: CreatePositionRequest)
        (cancellation: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! result =
                    db.QuerySingleAsync<Position>(
                        CommandDefinition(
                            "INSERT INTO positions (pipeline_id, symbol, entry_price, quantity, buy_order_id, status, created_at, updated_at)
                             VALUES (@PipelineId, @Symbol, @EntryPrice, @Quantity, @BuyOrderId, @Status, NOW(), NOW())
                             RETURNING *",
                            {|
                                PipelineId = request.PipelineId
                                Symbol = request.Symbol
                                EntryPrice = request.EntryPrice
                                Quantity = request.Quantity
                                BuyOrderId = request.BuyOrderId
                                Status = int request.Status
                            |},
                            cancellationToken = cancellation
                        )
                    )

                logger.LogDebug("Created new position for pipeline {PipelineId}", request.PipelineId)
                return Ok result
            with ex ->
                logger.LogError(ex, "Failed to create position for pipeline {PipelineId}", request.PipelineId)
                return Result.Error(Unexpected ex)
        }

    let private updatePosition
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (request: UpdatePositionRequest)
        (cancellation: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let closedAt =
                    match request.ClosedAt with
                    | Some dt -> box dt
                    | None -> null

                let! result =
                    db.QuerySingleAsync<Position>(
                        CommandDefinition(
                            "UPDATE positions
                             SET exit_price = @ExitPrice,
                                 sell_order_id = @SellOrderId,
                                 status = @Status,
                                 closed_at = @ClosedAt,
                                 updated_at = NOW()
                             WHERE id = @Id
                             RETURNING *",
                            {|
                                Id = request.Id
                                ExitPrice = request.ExitPrice
                                SellOrderId = request.SellOrderId
                                Status = int request.Status
                                ClosedAt = closedAt
                            |},
                            cancellationToken = cancellation
                        )
                    )

                logger.LogDebug("Updated position {PositionId}", request.Id)
                return Ok result
            with ex ->
                logger.LogError(ex, "Failed to update position {PositionId}", request.Id)
                return Result.Error(Unexpected ex)
        }


    let create (scopeFactory: IServiceScopeFactory) (loggerFactory: ILoggerFactory) : T =
        let logger = loggerFactory.CreateLogger("PositionRepository")

        {
            GetOpen = getOpen scopeFactory logger
            Create = createPosition scopeFactory logger
            Update = updatePosition scopeFactory logger
        }
