namespace Warehouse.Core.Repositories

open System
open System.Data
open System.Threading
open System.Threading.Tasks
open Dapper
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Warehouse.Core.Domain
open Warehouse.Core.Infrastructure
open Warehouse.Core.Infrastructure.Entities
open Warehouse.Core.Shared

module PipelineStepRepository =
    open Errors
    open Mappers

    type T =
        {
            GetById: int -> CancellationToken -> Task<Result<PipelineStep, ServiceError>>
            GetByPipelineId: int -> CancellationToken -> Task<Result<PipelineStep list, ServiceError>>
            Create: PipelineStep -> CancellationToken -> Task<Result<PipelineStep, ServiceError>>
            Update: PipelineStep -> CancellationToken -> Task<Result<PipelineStep, ServiceError>>
            Delete: int -> CancellationToken -> Task<Result<unit, ServiceError>>
            DeleteByPipelineId: int -> CancellationToken -> Task<Result<int, ServiceError>>
            SetEnabled: int -> bool -> CancellationToken -> Task<Result<unit, ServiceError>>
            UpdateOrder: int -> int -> CancellationToken -> Task<Result<unit, ServiceError>>
            ReorderSteps: int -> int list -> CancellationToken -> Task<Result<unit, ServiceError>>
            GetMaxOrder: int -> CancellationToken -> Task<Result<int, ServiceError>>
        }

    let private getById (scopeFactory: IServiceScopeFactory) (logger: ILogger) (id: int) (_: CancellationToken) =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! results =
                    db.QueryAsync<PipelineStepEntity>(
                        "SELECT * FROM pipeline_steps WHERE id = @Id LIMIT 1",
                        {| Id = id |}
                    )

                match results |> Seq.tryHead with
                | Some entity ->
                    logger.LogDebug("Retrieved step {Id}", id)
                    return Ok(toPipelineStep entity None)
                | None ->
                    logger.LogWarning("Step {Id} not found", id)
                    return Result.Error(NotFound $"Step with id {id}")
            with ex ->
                logger.LogError(ex, "Failed to get step {Id}", id)
                return Result.Error(Unexpected ex)
        }

    let private getByPipelineId
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (pipelineId: int)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! results =
                    db.QueryAsync<PipelineStepEntity>(
                        "SELECT * FROM pipeline_steps WHERE pipeline_details_id = @PipelineId ORDER BY \"order\"",
                        {| PipelineId = pipelineId |}
                    )

                let steps = results |> Seq.map (fun s -> toPipelineStep s None) |> Seq.toList
                logger.LogDebug("Retrieved {Count} steps for pipeline {PipelineId}", steps.Length, pipelineId)
                return Ok steps
            with ex ->
                logger.LogError(ex, "Failed to get steps for pipeline {PipelineId}", pipelineId)
                return Result.Error(Unexpected ex)
        }

    let private createStep
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (step: PipelineStep)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let now = DateTime.UtcNow
                let entity = fromPipelineStep { step with CreatedAt = now; UpdatedAt = now }

                let! result =
                    db.QuerySingleAsync<int>(
                        """INSERT INTO pipeline_steps
                           (pipeline_details_id, step_type_key, name, "order", is_enabled, parameters, created_at, updated_at)
                           VALUES (@PipelineDetailsId, @StepTypeKey, @Name, @Order, @IsEnabled, @Parameters, @CreatedAt, @UpdatedAt)
                           RETURNING id""",
                        entity
                    )

                logger.LogInformation("Created step {Id} for pipeline {PipelineId}", result, step.PipelineDetailsId)
                return Ok { step with Id = result; CreatedAt = now; UpdatedAt = now }
            with ex ->
                logger.LogError(ex, "Failed to create step for pipeline {PipelineId}", step.PipelineDetailsId)
                return Result.Error(Unexpected ex)
        }

    let private updateStep
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (step: PipelineStep)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let now = DateTime.UtcNow
                let entity = fromPipelineStep { step with UpdatedAt = now }

                let! rowsAffected =
                    db.ExecuteAsync(
                        """UPDATE pipeline_steps
                           SET step_type_key = @StepTypeKey, name = @Name, "order" = @Order,
                               is_enabled = @IsEnabled, parameters = @Parameters, updated_at = @UpdatedAt
                           WHERE id = @Id""",
                        entity
                    )

                if rowsAffected > 0 then
                    logger.LogInformation("Updated step {Id}", step.Id)
                    return Ok { step with UpdatedAt = now }
                else
                    logger.LogWarning("Step {Id} not found for update", step.Id)
                    return Result.Error(NotFound $"Step with id {step.Id}")
            with ex ->
                logger.LogError(ex, "Failed to update step {Id}", step.Id)
                return Result.Error(Unexpected ex)
        }

    let private deleteStep (scopeFactory: IServiceScopeFactory) (logger: ILogger) (id: int) (_: CancellationToken) =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! rowsAffected = db.ExecuteAsync("DELETE FROM pipeline_steps WHERE id = @Id", {| Id = id |})

                if rowsAffected > 0 then
                    logger.LogInformation("Deleted step {Id}", id)
                    return Ok()
                else
                    logger.LogWarning("Step {Id} not found for deletion", id)
                    return Result.Error(NotFound $"Step with id {id}")
            with ex ->
                logger.LogError(ex, "Failed to delete step {Id}", id)
                return Result.Error(Unexpected ex)
        }

    let private deleteByPipelineId
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (pipelineId: int)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! rowsAffected =
                    db.ExecuteAsync(
                        "DELETE FROM pipeline_steps WHERE pipeline_details_id = @PipelineId",
                        {| PipelineId = pipelineId |}
                    )

                logger.LogInformation("Deleted {Count} steps for pipeline {PipelineId}", rowsAffected, pipelineId)
                return Ok rowsAffected
            with ex ->
                logger.LogError(ex, "Failed to delete steps for pipeline {PipelineId}", pipelineId)
                return Result.Error(Unexpected ex)
        }

    let private setEnabled
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (stepId: int)
        (enabled: bool)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let now = DateTime.UtcNow

                let! rowsAffected =
                    db.ExecuteAsync(
                        "UPDATE pipeline_steps SET is_enabled = @IsEnabled, updated_at = @UpdatedAt WHERE id = @Id",
                        {| IsEnabled = enabled; UpdatedAt = now; Id = stepId |}
                    )

                if rowsAffected > 0 then
                    logger.LogInformation("Set step {Id} enabled = {Enabled}", stepId, enabled)
                    return Ok()
                else
                    logger.LogWarning("Step {Id} not found for enable/disable", stepId)
                    return Result.Error(NotFound $"Step with id {stepId}")
            with ex ->
                logger.LogError(ex, "Failed to set enabled for step {Id}", stepId)
                return Result.Error(Unexpected ex)
        }

    let private updateOrder
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (stepId: int)
        (order: int)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let now = DateTime.UtcNow

                let! rowsAffected =
                    db.ExecuteAsync(
                        "UPDATE pipeline_steps SET \"order\" = @Order, updated_at = @UpdatedAt WHERE id = @Id",
                        {| Order = order; UpdatedAt = now; Id = stepId |}
                    )

                if rowsAffected > 0 then
                    logger.LogDebug("Updated order for step {Id} to {Order}", stepId, order)
                    return Ok()
                else
                    logger.LogWarning("Step {Id} not found for order update", stepId)
                    return Result.Error(NotFound $"Step with id {stepId}")
            with ex ->
                logger.LogError(ex, "Failed to update order for step {Id}", stepId)
                return Result.Error(Unexpected ex)
        }

    let private reorderSteps
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (pipelineId: int)
        (stepIds: int list)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let now = DateTime.UtcNow

                // todo: optimize. for now it's simpler to implement like this
                let! _ =
                    db.ExecuteAsync(
                        // temp large offset
                        "UPDATE pipeline_steps SET \"order\" = -\"order\" - 10000 WHERE pipeline_details_id = @PipelineId",
                        {| PipelineId = pipelineId |}
                    )

                for i, stepId in stepIds |> List.indexed do
                    let! _ =
                        db.ExecuteAsync(
                            "UPDATE pipeline_steps SET \"order\" = @Order, updated_at = @UpdatedAt WHERE id = @Id AND pipeline_details_id = @PipelineId",
                            {| Order = i; UpdatedAt = now; Id = stepId; PipelineId = pipelineId |}
                        )

                    ()

                logger.LogInformation("Reordered {Count} steps for pipeline {PipelineId}", stepIds.Length, pipelineId)
                return Ok()
            with ex ->
                logger.LogError(ex, "Failed to reorder steps for pipeline {PipelineId}", pipelineId)
                return Result.Error(Unexpected ex)
        }

    let private getMaxOrder
        (scopeFactory: IServiceScopeFactory)
        (logger: ILogger)
        (pipelineId: int)
        (_: CancellationToken)
        =
        task {
            try
                use scope = scopeFactory.CreateScope()
                use db = scope.ServiceProvider.GetRequiredService<IDbConnection>()

                let! result =
                    db.QuerySingleOrDefaultAsync<Nullable<int>>(
                        "SELECT MAX(\"order\") FROM pipeline_steps WHERE pipeline_details_id = @PipelineId",
                        {| PipelineId = pipelineId |}
                    )

                let maxOrder = if result.HasValue then result.Value else -1
                logger.LogDebug("Max order for pipeline {PipelineId} is {MaxOrder}", pipelineId, maxOrder)
                return Ok maxOrder
            with ex ->
                logger.LogError(ex, "Failed to get max order for pipeline {PipelineId}", pipelineId)
                return Result.Error(Unexpected ex)
        }

    let create (scopeFactory: IServiceScopeFactory) : T =
        let loggerFactory = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILoggerFactory>()
        let logger = loggerFactory.CreateLogger("PipelineStepRepository")

        {
            GetById = getById scopeFactory logger
            GetByPipelineId = getByPipelineId scopeFactory logger
            Create = createStep scopeFactory logger
            Update = updateStep scopeFactory logger
            Delete = deleteStep scopeFactory logger
            DeleteByPipelineId = deleteByPipelineId scopeFactory logger
            SetEnabled = setEnabled scopeFactory logger
            UpdateOrder = updateOrder scopeFactory logger
            ReorderSteps = reorderSteps scopeFactory logger
            GetMaxOrder = getMaxOrder scopeFactory logger
        }
