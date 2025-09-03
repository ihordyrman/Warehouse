using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Endpoints;

public static class MarketDetailsEndpoints
{
    public static RouteGroupBuilder MapMarketDetailsEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/market");
        group.WithTags("market");
        group.RequireRateLimiting("MarketApiPolicy");

        group.MapGet(
                "/",
                async (WarehouseDbContext db) =>
                {
                    List<MarketDetailsWithCredentialsDto> marketsWithCredentials = await db.MarketDetails.AsNoTracking()
                        .Include(x => x.Credentials)
                        .ThenInclude(x => x.MarketDetails)
                        .Select(x => x.AsDtoWithCredentials())
                        .ToListAsync();
                    return TypedResults.Ok(marketsWithCredentials);
                })
            .WithName("GetAllMarketDetails")
            .WithSummary("Get all market details")
            .Produces<List<MarketDetailsDto>>()
            .Produces<List<MarketDetailsWithCredentialsDto>>();

        group.MapGet(
                "/{id:int}",
                async Task<Results<Ok<MarketDetailsWithCredentialsDto>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    MarketDetails? market = await db.MarketDetails.Include(x => x.Credentials)
                        .ThenInclude(x => x.MarketDetails)
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return market switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(market.AsDtoWithCredentials())
                    };
                })
            .WithName("GetMarketDetails")
            .WithSummary("Get market details by ID")
            .Produces<MarketDetailsWithCredentialsDto>()
            .Produces(404);

        group.MapPost(
                "/",
                async Task<Results<Created<MarketDetailsDto>, BadRequest<string>>> (
                    WarehouseDbContext db,
                    CreateMarketDetailsDto marketDetailsDto,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(marketDetailsDto);
                    bool duplicateExists = await db.MarketDetails.AnyAsync(x => x.Type == marketDetailsDto.Type);
                    if (duplicateExists)
                    {
                        return TypedResults.BadRequest("Market details already exist for this market type");
                    }

                    MarketDetails marketDetails = marketDetailsDto.AsEntity();

                    try
                    {
                        db.MarketDetails.Add(marketDetails);
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketDetailsAPI.Create");
                        logger.LogError(ex, "Failed to create market details for Type: {Type}", marketDetailsDto.Type);
                        return TypedResults.BadRequest("Failed to create market details");
                    }

                    return TypedResults.Created($"/market/{marketDetails.Id}", marketDetails.AsDto());
                })
            .WithName("CreateMarketDetails")
            .WithSummary("Create new market details")
            .Produces<MarketDetailsDto>(201)
            .Produces<string>(400);

        group.MapPut(
                "/{id:int}",
                async Task<Results<Ok, NotFound, BadRequest<string>>> (
                    WarehouseDbContext db,
                    int id,
                    MarketDetailsDto marketDetailsDto,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(marketDetailsDto);
                    if (id != marketDetailsDto.Id)
                    {
                        return TypedResults.BadRequest("ID mismatch");
                    }

                    bool marketExists = await db.MarketDetails.AnyAsync(x => x.Id == id);
                    if (!marketExists)
                    {
                        return TypedResults.NotFound();
                    }

                    bool duplicateExists = await db.MarketDetails.AnyAsync(x => x.Id != id && x.Type == marketDetailsDto.Type);
                    if (duplicateExists)
                    {
                        return TypedResults.BadRequest("Market details already exist for this market type");
                    }

                    try
                    {
                        int rowsAffected = await db.MarketDetails.Where(x => x.Id == id)
                            .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.Type, marketDetailsDto.Type));

                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketDetailsAPI.Update");
                        logger.LogError(ex, "Failed to update market details with ID: {Id}", id);
                        return TypedResults.BadRequest("Failed to update market details");
                    }
                })
            .WithName("UpdateMarketDetails")
            .WithSummary("Update market details")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        group.MapDelete(
                "/{id:int}",
                async Task<Results<NotFound, Ok, BadRequest<string>>> (WarehouseDbContext db, int id, ILoggerFactory loggerFactory) =>
                {
                    bool hasCredentials = await db.MarketCredentials.AnyAsync(x => x.MarketId == id);
                    if (hasCredentials)
                    {
                        return TypedResults.BadRequest("Cannot delete market details that have associated credentials");
                    }

                    try
                    {
                        int rowsAffected = await db.MarketDetails.Where(x => x.Id == id).ExecuteDeleteAsync();
                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketDetailsAPI.Delete");
                        logger.LogError(ex, "Failed to delete market details with ID: {Id}", id);
                        return TypedResults.BadRequest("Failed to delete market details");
                    }
                })
            .WithName("DeleteMarketDetails")
            .WithSummary("Delete market details")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        group.MapGet(
                "/{id:int}/credentials",
                async Task<Results<Ok<List<MarketCredentialsDto>>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    bool marketExists = await db.MarketDetails.AnyAsync(x => x.Id == id);
                    if (!marketExists)
                    {
                        return TypedResults.NotFound();
                    }

                    List<MarketCredentialsDto> credentials = await db.MarketCredentials.AsNoTracking()
                        .Include(x => x.MarketDetails)
                        .Where(x => x.MarketId == id)
                        .Select(x => x.AsDto())
                        .ToListAsync();

                    return TypedResults.Ok(credentials);
                })
            .WithName("GetMarketCredentialsByMarketId")
            .WithSummary("Get all credentials for a specific market")
            .Produces<List<MarketCredentialsDto>>()
            .Produces(404);

        return group;
    }
}
