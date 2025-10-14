using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Warehouse.Backend.Endpoints.Models;
using Warehouse.Backend.Endpoints.Validation;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Pipelines.Domain;

namespace Warehouse.Backend.Endpoints;

public static class PipelineStepEndpoints
{
    public static RouteGroupBuilder MapPipelineStepEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder workerGroup = routes.MapGroup("/worker/{workerId:int}/pipeline-steps");
        workerGroup.WithTags("pipeline-steps");
        workerGroup.RequireRateLimiting("ApiPolicy");

        RouteGroupBuilder stepGroup = routes.MapGroup("/pipeline-step");
        stepGroup.WithTags("pipeline-steps");
        stepGroup.RequireRateLimiting("ApiPolicy");

        workerGroup.MapGet(
                "/",
                async Task<Results<Ok<List<PipelineStepResponse>>, NotFound>> (WarehouseDbContext db, int workerId) =>
                {
                    if (!await db.WorkerDetails.AnyAsync(x => x.Id == workerId))
                    {
                        return TypedResults.NotFound();
                    }

                    List<PipelineStepResponse> steps = await db.PipelineSteps.Where(x => x.WorkerDetailsId == workerId)
                        .OrderBy(x => x.Order)
                        .Select(x => x.AsDto())
                        .ToListAsync();

                    return TypedResults.Ok(steps);
                })
            .WithName("GetPipelineStepsByWorkerId")
            .WithSummary("Get all pipeline steps for a specific worker")
            .Produces<List<PipelineStepResponse>>()
            .Produces(404);

        workerGroup.MapPost(
                "/",
                async Task<Results<Created<PipelineStepResponse>, NotFound, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    int workerId,
                    CreatePipelineStepRequest request,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(request);

                    if (!await db.WorkerDetails.AnyAsync(x => x.Id == workerId))
                    {
                        return TypedResults.NotFound();
                    }

                    if (await db.PipelineSteps.AnyAsync(x => x.WorkerDetailsId == workerId && x.Order == request.Order))
                    {
                        throw new ValidationException($"A pipeline step with order {request.Order} already exists for this worker");
                    }

                    PipelineStep step = request.AsEntity(workerId);

                    try
                    {
                        db.PipelineSteps.Add(step);
                        await db.SaveChangesAsync();

                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Create");
                        logger.LogInformation("Created pipeline step {StepId} for worker {WorkerId}", step.Id, workerId);

                        return TypedResults.Created($"/pipeline-step/{step.Id}", step.AsDto());
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Create");
                        logger.LogError(ex, "Failed to create pipeline step for worker {WorkerId}", workerId);
                        throw new ValidationException("Failed to create pipeline step. Please try again.");
                    }
                })
            .WithName("CreatePipelineStep")
            .WithSummary("Create a new pipeline step for a worker")
            .Produces<PipelineStepResponse>(201)
            .Produces(404)
            .ProducesValidationProblem();

        workerGroup.MapPut(
                "/reorder",
                async Task<Results<Ok, NotFound, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    int workerId,
                    ReorderPipelineStepsRequest request,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(request);

                    if (!await db.WorkerDetails.AnyAsync(x => x.Id == workerId))
                    {
                        return TypedResults.NotFound();
                    }

                    List<PipelineStep> steps = await db.PipelineSteps.Where(x => x.WorkerDetailsId == workerId).ToListAsync();

                    var stepIds = steps.Select(x => x.Id).ToHashSet();
                    foreach (ReorderPipelineStepsRequest.StepOrder stepOrder in
                             request.StepOrders.Where(stepOrder => !stepIds.Contains(stepOrder.StepId)))
                    {
                        throw new ValidationException($"Step {stepOrder.StepId} does not belong to worker {workerId}");
                    }

                    if (request.StepOrders.GroupBy(x => x.Order).Any(x => x.Count() > 1))
                    {
                        throw new ValidationException("Duplicate order values are not allowed");
                    }

                    try
                    {
                        foreach (ReorderPipelineStepsRequest.StepOrder stepOrder in request.StepOrders)
                        {
                            PipelineStep? step = steps.FirstOrDefault(x => x.Id == stepOrder.StepId);
                            if (step is null)
                            {
                                continue;
                            }

                            step.Order = stepOrder.Order;
                            step.UpdatedAt = DateTime.UtcNow;
                        }

                        await db.SaveChangesAsync();

                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Reorder");
                        logger.LogInformation("Reordered pipeline steps for worker {WorkerId}", workerId);

                        return TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Reorder");
                        logger.LogError(ex, "Failed to reorder pipeline steps for worker {WorkerId}", workerId);
                        throw new ValidationException("Failed to reorder pipeline steps. Please try again.");
                    }
                })
            .WithName("ReorderPipelineSteps")
            .WithSummary("Reorder pipeline steps for a worker")
            .Produces(200)
            .Produces(404)
            .ProducesValidationProblem();

        workerGroup.MapGet(
                "/enabled",
                async Task<Results<Ok<List<PipelineStepResponse>>, NotFound>> (WarehouseDbContext db, int workerId) =>
                {
                    if (!await db.WorkerDetails.AnyAsync(x => x.Id == workerId))
                    {
                        return TypedResults.NotFound();
                    }

                    List<PipelineStepResponse> steps = await db.PipelineSteps.Where(x => x.WorkerDetailsId == workerId && x.IsEnabled)
                        .OrderBy(x => x.Order)
                        .Select(x => x.AsDto())
                        .ToListAsync();

                    return TypedResults.Ok(steps);
                })
            .WithName("GetEnabledPipelineSteps")
            .WithSummary("Get enabled pipeline steps for a worker")
            .Produces<List<PipelineStepResponse>>()
            .Produces(404);

        stepGroup.MapGet(
                "/{id:int}",
                async Task<Results<Ok<PipelineStepResponse>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    PipelineStep? step = await db.PipelineSteps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                    return step switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(step.AsDto())
                    };
                })
            .WithName("GetPipelineStepById")
            .WithSummary("Get a pipeline step by its ID")
            .Produces<PipelineStepResponse>()
            .Produces(404);

        stepGroup.MapPut(
                "/{id:int}",
                async Task<Results<Ok<PipelineStepResponse>, NotFound, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    int id,
                    UpdatePipelineStepRequest request,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(request);

                    PipelineStep? step = await db.PipelineSteps.FirstOrDefaultAsync(x => x.Id == id);
                    if (step is null)
                    {
                        return TypedResults.NotFound();
                    }

                    if (request.Order.HasValue && request.Order.Value != step.Order)
                    {
                        if (await db.PipelineSteps.AnyAsync(x => x.WorkerDetailsId == step.WorkerDetailsId &&
                                                                 x.Order == request.Order.Value &&
                                                                 x.Id != id))
                        {
                            throw new ValidationException(
                                $"A pipeline step with order {request.Order.Value} already exists for this worker");
                        }
                    }

                    step.UpdateFrom(request);

                    try
                    {
                        await db.SaveChangesAsync();

                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Update");
                        logger.LogInformation("Updated pipeline step {StepId}", id);

                        return TypedResults.Ok(step.AsDto());
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Update");
                        logger.LogError(ex, "Failed to update pipeline step {StepId}", id);
                        throw new ValidationException("Failed to update pipeline step. Please try again.");
                    }
                })
            .WithName("UpdatePipelineStep")
            .WithSummary("Update an existing pipeline step")
            .Produces<PipelineStepResponse>()
            .Produces(404)
            .ProducesValidationProblem();

        stepGroup.MapPatch(
                "/{id:int}/toggle-enabled",
                async Task<Results<Ok<PipelineStepResponse>, NotFound>> (WarehouseDbContext db, int id, ILoggerFactory loggerFactory) =>
                {
                    PipelineStep? step = await db.PipelineSteps.FirstOrDefaultAsync(x => x.Id == id);
                    if (step is null)
                    {
                        return TypedResults.NotFound();
                    }

                    step.IsEnabled = !step.IsEnabled;
                    step.UpdatedAt = DateTime.UtcNow;

                    try
                    {
                        await db.SaveChangesAsync();

                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Toggle");
                        logger.LogInformation("Toggled enabled state for pipeline step {StepId} to {IsEnabled}", id, step.IsEnabled);

                        return TypedResults.Ok(step.AsDto());
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Toggle");
                        logger.LogError(ex, "Failed to toggle pipeline step {StepId}", id);
                        return TypedResults.NotFound();
                    }
                })
            .WithName("TogglePipelineStepEnabled")
            .WithSummary("Toggle the enabled state of a pipeline step")
            .Produces<PipelineStepResponse>()
            .Produces(404);

        stepGroup.MapDelete(
                "/{id:int}",
                async Task<Results<NoContent, NotFound>> (WarehouseDbContext db, int id, ILoggerFactory loggerFactory) =>
                {
                    PipelineStep? step = await db.PipelineSteps.FirstOrDefaultAsync(x => x.Id == id);
                    if (step is null)
                    {
                        return TypedResults.NotFound();
                    }

                    int workerId = step.WorkerDetailsId;
                    int deletedOrder = step.Order;

                    db.PipelineSteps.Remove(step);
                    await db.SaveChangesAsync();

                    List<PipelineStep> remainingSteps = await db.PipelineSteps
                        .Where(x => x.WorkerDetailsId == workerId && x.Order > deletedOrder)
                        .ToListAsync();

                    foreach (PipelineStep remainingStep in remainingSteps)
                    {
                        remainingStep.Order--;
                        remainingStep.UpdatedAt = DateTime.UtcNow;
                    }

                    if (remainingSteps.Any())
                    {
                        await db.SaveChangesAsync();
                    }

                    ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.Delete");
                    logger.LogInformation("Deleted pipeline step {StepId} and reordered remaining steps", id);

                    return TypedResults.NoContent();
                })
            .WithName("DeletePipelineStep")
            .WithSummary("Delete a pipeline step")
            .Produces(204)
            .Produces(404);

        workerGroup.MapPut(
                "/",
                async Task<Results<Ok<List<PipelineStepResponse>>, NotFound, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    int workerId,
                    List<CreatePipelineStepRequest> requests,
                    ILoggerFactory loggerFactory) =>
                {
                    foreach (CreatePipelineStepRequest request in requests)
                    {
                        ValidationHelper.ValidateAndThrow(request);
                    }

                    if (!await db.WorkerDetails.AnyAsync(x => x.Id == workerId))
                    {
                        return TypedResults.NotFound();
                    }

                    if (requests.GroupBy(x => x.Order).Any(x => x.Count() > 1))
                    {
                        throw new ValidationException("Duplicate order values are not allowed");
                    }

                    await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
                    try
                    {
                        await db.PipelineSteps.Where(x => x.WorkerDetailsId == workerId).ExecuteDeleteAsync();

                        var newSteps = requests.Select(x => x.AsEntity(workerId)).ToList();
                        db.PipelineSteps.AddRange(newSteps);
                        await db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.BulkUpdate");
                        logger.LogInformation(
                            "Bulk updated pipeline steps for worker {WorkerId}. Added {Count} steps",
                            workerId,
                            newSteps.Count);

                        return TypedResults.Ok(newSteps.Select(s => s.AsDto()).ToList());
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        ILogger logger = loggerFactory.CreateLogger("PipelineStepAPI.BulkUpdate");
                        logger.LogError(ex, "Failed to bulk update pipeline steps for worker {WorkerId}", workerId);
                        throw new ValidationException("Failed to update pipeline steps. Please try again.");
                    }
                })
            .WithName("BulkUpdatePipelineSteps")
            .WithSummary("Replace all pipeline steps for a worker")
            .Produces<List<PipelineStepResponse>>()
            .Produces(404)
            .ProducesValidationProblem();

        return stepGroup;
    }
}
