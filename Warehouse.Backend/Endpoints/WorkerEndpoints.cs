using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Endpoints;

public static class WorkerEndpoints
{
    public static RouteGroupBuilder MapWorkerEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/worker");
        group.WithTags("workers");
        group.RequireRateLimiting("ApiPolicy");

        group.MapGet(
                "/",
                async (WarehouseDbContext db)
                    => TypedResults.Ok(await db.WorkerDetails.AsNoTracking().Select(x => x.AsDto()).ToListAsync()))
            .WithName("GetAllWorkers")
            .WithSummary("Get all worker configurations")
            .Produces<List<WorkerDto>>();

        group.MapGet(
                "/{id:int}",
                async Task<Results<Ok<WorkerDto>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    WorkerDetails? worker = await db.WorkerDetails.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    return worker switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(worker.AsDto())
                    };
                })
            .WithName("GetWorkerById")
            .WithSummary("Get a worker by its ID")
            .Produces<WorkerDto>()
            .Produces(404);

        group.MapGet(
                "/by-market/{marketType}",
                async Task<Results<Ok<List<WorkerDto>>, BadRequest<ValidationProblemDetails>>> (WarehouseDbContext db, string marketType) =>
                {
                    if (!Enum.TryParse(marketType, true, out MarketType type))
                    {
                        throw new ValidationException($"Invalid market type: {marketType}");
                    }

                    return TypedResults.Ok(
                        await db.WorkerDetails.AsNoTracking().Where(x => x.Type == type).Select(x => x.AsDto()).ToListAsync());
                })
            .WithName("GetWorkersByMarketType")
            .WithSummary("Get all workers for a specific market type")
            .Produces<List<WorkerDto>>()
            .Produces<string>(400);

        group.MapGet(
                "/enabled",
                async (WarehouseDbContext db) => TypedResults.Ok(
                    await db.WorkerDetails.AsNoTracking().Where(x => x.Enabled).Select(x => x.AsDto()).ToListAsync()))
            .WithName("GetEnabledWorkers")
            .WithSummary("Get all enabled workers")
            .Produces<List<WorkerDto>>();

        group.MapPost(
                "/",
                async Task<Results<Created<WorkerDto>, NotFound, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    CreateWorkerDto dto,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(dto);

                    if (await db.WorkerDetails.AnyAsync(x => x.Type == dto.Type && x.Symbol == dto.Symbol.ToUpperInvariant()))
                    {
                        throw new ValidationException($"Worker configuration for {dto.Type}/{dto.Symbol} already exists");
                    }

                    WorkerDetails worker = dto.AsEntity();

                    try
                    {
                        db.WorkerDetails.Add(worker);
                        await db.SaveChangesAsync();

                        ILogger logger = loggerFactory.CreateLogger("WorkerAPI.Create");
                        logger.LogInformation("Created worker {WorkerId} for {Type}/{Symbol}", worker.Id, worker.Type, worker.Symbol);
                        return TypedResults.Created($"/worker/{worker.Id}", worker.AsDto());
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("WorkerAPI.Create");
                        logger.LogError(ex, "Failed to create worker");
                        throw new ValidationException("Failed to create worker. Please try again.");
                    }
                })
            .WithName("CreateWorker")
            .WithSummary("Create a new worker configuration")
            .Produces<WorkerDto>(201)
            .ProducesValidationProblem();

        group.MapPut(
                "/{id:int}",
                async Task<Results<Ok<WorkerDto>, NotFound, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    int id,
                    UpdateWorkerDto dto,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(dto);
                    WorkerDetails? worker = await db.WorkerDetails.FirstOrDefaultAsync(x => x.Id == id);

                    if (worker == null)
                    {
                        return TypedResults.NotFound();
                    }

                    if (dto.Type.HasValue || !string.IsNullOrWhiteSpace(dto.Symbol))
                    {
                        MarketType checkType = dto.Type ?? worker.Type;
                        string checkSymbol = (!string.IsNullOrWhiteSpace(dto.Symbol) ? dto.Symbol : worker.Symbol).ToUpperInvariant();
                        if (await db.WorkerDetails.AnyAsync(x => x.Id != id && x.Type == checkType && x.Symbol == checkSymbol))
                        {
                            throw new ValidationException($"Worker configuration for {checkType}/{checkSymbol} already exists");
                        }
                    }

                    worker.UpdateFrom(dto);

                    try
                    {
                        await db.SaveChangesAsync();

                        ILogger logger = loggerFactory.CreateLogger("WorkerAPI.Update");
                        logger.LogInformation("Updated worker {Id}", id);
                        return TypedResults.Ok(worker.AsDto());
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("WorkerAPI.Update");
                        logger.LogError(ex, "Failed to update worker {Id}", id);
                        throw new ValidationException("Failed to update worker. Please try again.");
                    }
                })
            .WithName("UpdateWorker")
            .WithSummary("Update an existing worker configuration")
            .Produces<WorkerDto>()
            .Produces(404)
            .ProducesValidationProblem();

        group.MapDelete(
                "/{id:int}",
                async Task<Results<NoContent, NotFound>> (WarehouseDbContext db, int id, ILoggerFactory loggerFactory) =>
                {
                    WorkerDetails? worker = await db.WorkerDetails.FirstOrDefaultAsync(x => x.Id == id);

                    if (worker == null)
                    {
                        return TypedResults.NotFound();
                    }

                    db.WorkerDetails.Remove(worker);
                    await db.SaveChangesAsync();

                    ILogger logger = loggerFactory.CreateLogger("WorkerAPI.Delete");
                    logger.LogInformation("Deleted worker {Id}", id);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteWorker")
            .WithSummary("Delete a worker configuration")
            .Produces(204)
            .Produces(404);

        return group;
    }
}
