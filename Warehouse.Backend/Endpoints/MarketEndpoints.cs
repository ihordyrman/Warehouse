using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Endpoints;

public static class MarketEndpoints
{
    public static RouteGroupBuilder MapMarketEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/market");

        group.WithTags("market");
        group.RequireRateLimiting("MarketApiPolicy");

        group.MapGet(
            "/",
            async (WarehouseDbContext db) =>
            {
                List<MarketCredentialsDto> markets = await db.Markets.AsNoTracking().Select(m => m.AsDto()).ToListAsync();
                return TypedResults.Ok(markets);
            });

        group.MapGet(
            "/{id:int}",
            async Task<Results<Ok<MarketCredentialsDto>, NotFound>> (WarehouseDbContext db, int id) =>
            {
                MarketCredentials? market = await db.Markets.FindAsync(id);
                return market switch
                {
                    null => TypedResults.NotFound(),
                    _ => TypedResults.Ok(market.AsDto())
                };
            });

        group.MapPost(
            "/",
            async Task<Results<Created<MarketCredentialsDto>, BadRequest>> (
                WarehouseDbContext db,
                CreateMarketCredentialsDto marketCredentialsDto,
                ILoggerFactory loggerFactory) =>
            {
                MarketCredentials marketCredentials = marketCredentialsDto.AsEntity();
                try
                {
                    db.Markets.Add(marketCredentials);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    ILogger logger = loggerFactory.CreateLogger("MarketAPI.Create");
                    logger.LogError("Failed to create market: {Error}", ex.InnerException?.Message ?? ex.Message);
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/market/{marketCredentials.Id}", marketCredentials.AsDto());
            });

        group.MapPut(
            "/{id:int}",
            async Task<Results<Ok, NotFound, BadRequest>> (WarehouseDbContext db, int id, MarketCredentialsDto marketCredentialsDto) =>
            {
                if (id != marketCredentialsDto.Id)
                {
                    return TypedResults.BadRequest();
                }

                int rowsAffected = await db.Markets.Where(m => m.Id == id)
                    .ExecuteUpdateAsync(updates => updates.SetProperty(m => m.Type, marketCredentialsDto.Type)
                                            .SetProperty(m => m.ApiKey, marketCredentialsDto.ApiKey)
                                            .SetProperty(m => m.Passphrase, marketCredentialsDto.Passphrase)
                                            .SetProperty(m => m.SecretKey, marketCredentialsDto.SecretKey)
                                            .SetProperty(m => m.IsDemo, marketCredentialsDto.IsDemo));

                return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
            });

        group.MapDelete(
            "/{id:int}",
            async Task<Results<NotFound, Ok>> (WarehouseDbContext db, int id) =>
            {
                int rowsAffected = await db.Markets.Where(m => m.Id == id).ExecuteDeleteAsync();

                return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
            });

        return group;
    }
}
