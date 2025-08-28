using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Entities;
using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Features.Endpoints;

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
                List<MarketDto> markets = await db.Markets.AsNoTracking().Select(m => m.AsDto()).ToListAsync();
                return TypedResults.Ok(markets);
            });

        group.MapGet(
            "/{id:int}",
            async Task<Results<Ok<MarketDto>, NotFound>> (WarehouseDbContext db, int id) =>
            {
                Market? market = await db.Markets.FindAsync(id);
                return market switch
                {
                    null => TypedResults.NotFound(),
                    _ => TypedResults.Ok(market.AsDto())
                };
            });

        group.MapPost(
            "/",
            async Task<Results<Created<MarketDto>, BadRequest>> (
                WarehouseDbContext db,
                CreateMarketDto marketDto,
                ILoggerFactory loggerFactory) =>
            {
                Market market = marketDto.AsEntity();
                try
                {
                    db.Markets.Add(market);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    ILogger logger = loggerFactory.CreateLogger("MarketAPI.Create");
                    logger.LogError("Failed to create market: {Error}", ex.InnerException?.Message ?? ex.Message);
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/market/{market.Id}", market.AsDto());
            });

        group.MapPut(
            "/{id:int}",
            async Task<Results<Ok, NotFound, BadRequest>> (WarehouseDbContext db, int id, MarketDto marketDto) =>
            {
                if (id != marketDto.Id)
                {
                    return TypedResults.BadRequest();
                }

                int rowsAffected = await db.Markets.Where(m => m.Id == id)
                    .ExecuteUpdateAsync(updates => updates.SetProperty(m => m.Type, marketDto.Type)
                                            .SetProperty(m => m.ApiKey, marketDto.ApiKey)
                                            .SetProperty(m => m.Passphrase, marketDto.Passphrase)
                                            .SetProperty(m => m.SecretKey, marketDto.SecretKey)
                                            .SetProperty(m => m.IsDemo, marketDto.IsDemo));

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
