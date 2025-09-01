using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Endpoints;

public static class MarketCredentialsEndpoints
{
    public static RouteGroupBuilder MapMarketCredentialsEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/market");
        group.WithTags("market");
        group.RequireRateLimiting("MarketApiPolicy");

        group.MapGet(
                "/",
                async (WarehouseDbContext db) =>
                {
                    List<MarketCredentialsDto> markets = await db.MarketCredentials.AsNoTracking().Select(x => x.AsDto()).ToListAsync();
                    return TypedResults.Ok(markets);
                })
            .WithName("GetAllMarketCredentials")
            .WithSummary("Get all market credentials")
            .Produces<List<MarketCredentialsDto>>();

        group.MapGet(
                "/{id:int}",
                async Task<Results<Ok<MarketCredentialsDto>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    MarketCredentialsDto? market = await db.MarketCredentials.Include(x => x.MarketDetails)
                        .Where(x => x.Id == id)
                        .Select(x => x.AsDto())
                        .FirstOrDefaultAsync();

                    return market switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(market)
                    };
                })
            .WithName("GetMarketCredentials")
            .WithSummary("Get market credentials by ID")
            .Produces<MarketCredentialsDto>()
            .Produces(404);

        group.MapPost(
                "/",
                async Task<Results<Created<MarketCredentialsDto>, BadRequest<string>>> (
                    WarehouseDbContext db,
                    CreateMarketCredentialsDto marketCredentialsDto,
                    ILoggerFactory loggerFactory) =>
                {
                    bool marketExists = await db.MarketDetails.AnyAsync(m => m.Id == marketCredentialsDto.MarketId);
                    if (!marketExists)
                    {
                        return TypedResults.BadRequest("Market not found");
                    }

                    bool duplicateExists =
                        await db.MarketCredentials.AnyAsync(x => x.MarketId == marketCredentialsDto.MarketId &&
                                                                 x.ApiKey == marketCredentialsDto.ApiKey);
                    if (duplicateExists)
                    {
                        return TypedResults.BadRequest("Credentials already exist for this market");
                    }

                    MarketCredentials marketCredentials = marketCredentialsDto.AsEntity();

                    try
                    {
                        db.MarketCredentials.Add(marketCredentials);
                        await db.SaveChangesAsync();
                        await db.Entry(marketCredentials).Reference(m => m.MarketDetails).LoadAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAPI.Create");
                        logger.LogError(ex, "Failed to create market credentials for MarketId: {MarketId}", marketCredentialsDto.MarketId);
                        return TypedResults.BadRequest("Failed to create market credentials");
                    }

                    return TypedResults.Created($"/market/{marketCredentials.Id}", marketCredentials.AsDto());
                })
            .WithName("CreateMarketCredentials")
            .WithSummary("Create new market credentials")
            .Produces<MarketCredentialsDto>(201)
            .Produces<string>(400);

        group.MapPut(
                "/{id:int}",
                async Task<Results<Ok, NotFound, BadRequest<string>>> (
                    WarehouseDbContext db,
                    int id,
                    MarketCredentialsDto marketCredentialsDto,
                    ILoggerFactory loggerFactory) =>
                {
                    if (id != marketCredentialsDto.Id)
                    {
                        return TypedResults.BadRequest("ID mismatch");
                    }

                    bool credentialsExist = await db.MarketCredentials.AnyAsync(x => x.Id == id);
                    if (!credentialsExist)
                    {
                        return TypedResults.NotFound();
                    }

                    bool marketExists = await db.MarketDetails.AnyAsync(x => x.Id == marketCredentialsDto.MarketId);
                    if (!marketExists)
                    {
                        return TypedResults.BadRequest("Market not found");
                    }

                    bool duplicateExists =
                        await db.MarketCredentials.AnyAsync(x => x.Id != id &&
                                                                 x.MarketId == marketCredentialsDto.MarketId &&
                                                                 x.ApiKey == marketCredentialsDto.ApiKey);
                    if (duplicateExists)
                    {
                        return TypedResults.BadRequest("Credentials already exist for this market");
                    }

                    try
                    {
                        int rowsAffected = await db.MarketCredentials.Where(x => x.Id == id)
                            .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.MarketId, marketCredentialsDto.MarketId)
                                                    .SetProperty(x => x.ApiKey, marketCredentialsDto.ApiKey)
                                                    .SetProperty(x => x.Passphrase, marketCredentialsDto.Passphrase)
                                                    .SetProperty(x => x.SecretKey, marketCredentialsDto.SecretKey)
                                                    .SetProperty(x => x.IsDemo, marketCredentialsDto.IsDemo));

                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAPI.Update");
                        logger.LogError(ex, "Failed to update market credentials with ID: {Id}", id);
                        return TypedResults.BadRequest("Failed to update market credentials");
                    }
                })
            .WithName("UpdateMarketCredentials")
            .WithSummary("Update market credentials")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        group.MapDelete(
                "/{id:int}",
                async Task<Results<NotFound, Ok, BadRequest<string>>> (WarehouseDbContext db, int id, ILoggerFactory loggerFactory) =>
                {
                    try
                    {
                        int rowsAffected = await db.MarketCredentials.Where(x => x.Id == id).ExecuteDeleteAsync();
                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAPI.Delete");
                        logger.LogError(ex, "Failed to delete market credentials with ID: {Id}", id);
                        return TypedResults.BadRequest("Failed to delete market credentials. It may be in use.");
                    }
                })
            .WithName("DeleteMarketCredentials")
            .WithSummary("Delete market credentials")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        return group;
    }
}
