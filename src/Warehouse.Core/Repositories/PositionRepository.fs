namespace Warehouse.Core.Repositories

open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Infrastructure
open Warehouse.Core.Infrastructure.Entities
open Warehouse.Core.Shared.Errors

module PositionRepository =
    open Mappers

    type T = { GetOpenPosition: int -> CancellationToken -> Task<Result<Option<Position>, ServiceError>> }

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

                let! results =
                    db.QueryAsync<PositionEntity>(
                        CommandDefinition(
                            "SELECT *
                              FROM positions
                              WHERE pipeline_id = @PipelineId AND status = @Status
                              LIMIT 1",
                            {| PipelineId = pipelineId; Status = int PositionStatus.Open |},
                            cancellationToken = cancellation
                        )
                    )

                match results |> Seq.tryHead with
                | None ->
                    logger.LogDebug("No open position found for pipeline {PipelineId}", pipelineId)
                    return Ok None
                | Some entity ->
                    logger.LogDebug("Retrieved open position for pipeline {PipelineId}", pipelineId)
                    return Ok(Some(toPosition entity))
            with ex ->
                logger.LogError(ex, "Failed to get open position for pipeline {PipelineId}", pipelineId)
                return Result.Error(Unexpected ex)
        }

    let create (scopeFactory: IServiceScopeFactory) (loggerFactory: ILoggerFactory) : T =
        let logger = loggerFactory.CreateLogger("PositionRepository")

        { GetOpenPosition = getOpen scopeFactory logger }
