using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;
using Warehouse.Backend.Core.Models.Endpoints;
using Warehouse.Backend.Endpoints.Validation;

namespace Warehouse.Backend.Endpoints;

public static class MarketCredentialsEndpoints
{
    public static RouteGroupBuilder MapMarketCredentialsEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/market/{id:int}/credential");
        group.WithTags("market-credentials");
        group.RequireRateLimiting("ApiPolicy");

        group.MapGet(
                "/",
                async Task<Results<Ok<MarketCredentialsDto>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    MarketCredentialsDto? credential = await db.MarketCredentials.Include(x => x.MarketDetails)
                        .Where(x => x.MarketId == id)
                        .Select(x => x.AsDto())
                        .FirstOrDefaultAsync();

                    return credential switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(credential)
                    };
                })
            .WithName("GetMarketCredential")
            .WithSummary("Get market credential by market ID")
            .Produces<MarketCredentialsDto>()
            .Produces(404);

        group.MapPost(
                "/",
                async Task<Results<Created<MarketCredentialsDto>, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    CreateMarketCredentialsDto marketCredentialsDto,
                    ILoggerFactory loggerFactory,
                    int id) =>
                {
                    ValidationHelper.ValidateAndThrow(marketCredentialsDto);
                    if (!await db.MarketDetails.AnyAsync(x => x.Id == id))
                    {
                        throw new ValidationException("Market not found");
                    }

                    if (await db.MarketCredentials.AnyAsync(x => x.MarketId == id))
                    {
                        throw new ValidationException("Credentials already exist for this market");
                    }

                    MarketCredentials marketCredentials = marketCredentialsDto.AsEntity(id);

                    try
                    {
                        db.MarketCredentials.Add(marketCredentials);
                        await db.SaveChangesAsync();
                        await db.Entry(marketCredentials).Reference(x => x.MarketDetails).LoadAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketCredentialsAPI.Create");
                        logger.LogError(ex, "Failed to create market credentials for MarketId: {MarketId}", id);
                        throw new ValidationException("Failed to create market credentials");
                    }

                    return TypedResults.Created($"/market/{id}/credential", marketCredentials.AsDto());
                })
            .WithName("CreateMarketCredential")
            .WithSummary("Create new market credential")
            .Produces<MarketCredentialsDto>(201)
            .Produces<string>(400);

        group.MapPut(
                "/",
                async Task<Results<Ok, NotFound, BadRequest<ValidationProblemDetails>, BadRequest>> (
                    WarehouseDbContext db,
                    int id,
                    UpdateMarketCredentialsDto marketCredentialsDto,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(marketCredentialsDto);
                    if (!await db.MarketDetails.AnyAsync(x => x.Id == id))
                    {
                        throw new ValidationException("Market not found");
                    }

                    if (!await db.MarketCredentials.AnyAsync(x => x.MarketId == id))
                    {
                        return TypedResults.NotFound();
                    }

                    try
                    {
                        int rowsAffected = await db.MarketCredentials.Where(x => x.MarketId == id)
                            .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.ApiKey, marketCredentialsDto.ApiKey)
                                                    .SetProperty(x => x.Passphrase, marketCredentialsDto.Passphrase)
                                                    .SetProperty(x => x.SecretKey, marketCredentialsDto.SecretKey));

                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketCredentialsAPI.Update");
                        logger.LogError(ex, "Failed to update market credentials for MarketId: {MarketId}", id);
                        return TypedResults.BadRequest();
                    }
                })
            .WithName("UpdateMarketCredential")
            .WithSummary("Update market credential")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        group.MapDelete(
                "/",
                async Task<Results<NotFound, Ok, BadRequest>> (WarehouseDbContext db, int id, ILoggerFactory loggerFactory) =>
                {
                    try
                    {
                        int rowsAffected = await db.MarketCredentials.Where(x => x.MarketId == id).ExecuteDeleteAsync();
                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketCredentialsAPI.Delete");
                        logger.LogError(ex, "Failed to delete market credentials for MarketId: {MarketId}", id);
                        return TypedResults.BadRequest();
                    }
                })
            .WithName("DeleteMarketCredential")
            .WithSummary("Delete market credential")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        return group;
    }
}
