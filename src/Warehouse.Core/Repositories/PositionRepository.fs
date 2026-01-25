namespace Warehouse.Core.Repositories

open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Shared.Errors

module PositionRepository =

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

    let create (scopeFactory: IServiceScopeFactory) (loggerFactory: ILoggerFactory) : T =
        let logger = loggerFactory.CreateLogger("PositionRepository")

        { GetOpenPosition = getOpen scopeFactory logger }
