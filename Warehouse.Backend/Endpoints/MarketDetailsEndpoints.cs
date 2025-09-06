using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Endpoints;

public static class MarketEndpoints
{
    public static RouteGroupBuilder MapMarketEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/market");
        group.WithTags("market");
        group.RequireRateLimiting("ApiPolicy");

        group.MapGet(
                "/",
                async (WarehouseDbContext db) =>
                {
                    List<MarketDto> marketsWithCredentials = await db.MarketDetails.AsNoTracking()
                        .Select(x => x.AsDto())
                        .ToListAsync();
                    return TypedResults.Ok(marketsWithCredentials);
                })
            .WithName("GetAllMarkets")
            .WithSummary("Get all markets")
            .Produces<List<MarketDto>>()
            .Produces<List<MarketDto>>();

        group.MapGet(
                "/{id:int}",
                async Task<Results<Ok<MarketDto>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    MarketDetails? market = await db.MarketDetails.FirstOrDefaultAsync(x => x.Id == id);
                    return market switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(market.AsDto())
                    };
                })
            .WithName("GetMarket")
            .WithSummary("Get market by ID")
            .Produces<MarketDto>()
            .Produces(404);

        group.MapPost(
                "/",
                async Task<Results<Created<MarketDto>, BadRequest<ValidationProblemDetails>, BadRequest>> (
                    WarehouseDbContext db,
                    CreateMarketDto marketDto,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(marketDto);
                    if (await db.MarketDetails.AnyAsync(x => x.Type == marketDto.Type))
                    {
                        throw new ValidationException("Market already exist for this market type");
                    }

                    MarketDetails marketDetails = marketDto.AsEntity();

                    try
                    {
                        db.MarketDetails.Add(marketDetails);
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAPI.Create");
                        logger.LogError(ex, "Failed to create market for Type: {Type}", marketDto.Type);
                        return TypedResults.BadRequest();
                    }

                    return TypedResults.Created($"/market/{marketDetails.Id}", marketDetails.AsDto());
                })
            .WithName("CreateMarket")
            .WithSummary("Create new market")
            .Produces<MarketDto>(201)
            .Produces<string>(400);

        group.MapPut(
                "/{id:int}",
                async Task<Results<Ok, NotFound, BadRequest<ValidationProblemDetails>, BadRequest>> (
                    WarehouseDbContext db,
                    int id,
                    UpdateMarketDto marketDto,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(marketDto);
                    if (!await db.MarketDetails.AnyAsync(x => x.Id == id))
                    {
                        return TypedResults.NotFound();
                    }

                    if (await db.MarketDetails.AnyAsync(x => x.Id != id && x.Type == marketDto.Type))
                    {
                        throw new ValidationException("Market already exist for this market type");
                    }

                    try
                    {
                        int rowsAffected = await db.MarketDetails.Where(x => x.Id == id)
                            .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.Type, marketDto.Type));
                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAPI.Update");
                        logger.LogError(ex, "Failed to update market with ID: {Id}", id);
                        return TypedResults.BadRequest();
                    }
                })
            .WithName("UpdateMarket")
            .WithSummary("Update market")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        group.MapDelete(
                "/{id:int}",
                async Task<Results<NotFound, Ok, BadRequest<ValidationProblemDetails>, BadRequest>> (
                    WarehouseDbContext db,
                    int id,
                    ILoggerFactory loggerFactory) =>
                {
                    if (await db.MarketCredentials.AnyAsync(x => x.MarketId == id))
                    {
                        throw new ValidationException("Cannot delete market that have associated credentials");
                    }

                    try
                    {
                        int rowsAffected = await db.MarketDetails.Where(x => x.Id == id).ExecuteDeleteAsync();
                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAPI.Delete");
                        logger.LogError(ex, "Failed to delete market with ID: {Id}", id);
                        return TypedResults.BadRequest();
                    }
                })
            .WithName("DeleteMarketDetails")
            .WithSummary("Delete market")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        group.MapGet(
                "/{id:int}/credentials",
                async Task<Results<Ok<MarketCredentialsDto>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    MarketCredentialsDto? credentials = await db.MarketCredentials.AsNoTracking()
                        .Where(x => x.MarketId == id)
                        .Select(x => x.AsDto())
                        .FirstOrDefaultAsync();

                    return credentials switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(credentials)
                    };
                })
            .WithName("GetMarketCredentialsByMarketId")
            .WithSummary("Get all credentials for a specific market")
            .Produces<List<MarketCredentialsDto>>()
            .Produces(404);

        return group;
    }
}
