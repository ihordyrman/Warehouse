using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Endpoints.Models;
using Warehouse.Backend.Endpoints.Validation;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;

namespace Warehouse.Backend.Endpoints;

public static class MarketAccountEndpoints
{
    public static RouteGroupBuilder MapMarketAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/account");
        group.WithTags("market-account");
        group.RequireRateLimiting("ApiPolicy");

        group.MapGet(
                "/",
                async Task<Results<Ok<List<MarketAccountResponse>>, NotFound>> (WarehouseDbContext db) =>
                {
                    List<MarketAccountResponse>? account = await db.MarketAccounts.Include(x => x.MarketDetails)
                        .Select(x => x.AsDto())
                        .ToListAsync();

                    return account switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(account)
                    };
                })
            .WithName("GetMarketAccounts")
            .WithSummary("Get all market accounts")
            .Produces<List<MarketAccountResponse>>();

        group.MapGet(
                "/{id:int}",
                async Task<Results<Ok<MarketAccountResponse>, NotFound>> (WarehouseDbContext db, int id) =>
                {
                    MarketAccountResponse? account = await db.MarketAccounts.Include(x => x.MarketDetails)
                        .Where(x => x.MarketId == id)
                        .Select(x => x.AsDto())
                        .FirstOrDefaultAsync();

                    return account switch
                    {
                        null => TypedResults.NotFound(),
                        _ => TypedResults.Ok(account)
                    };
                })
            .WithName("GetMarketCredential")
            .WithSummary("Get market account by market ID")
            .Produces<MarketAccountResponse>()
            .Produces(404);

        group.MapPost(
                "/{id:int}",
                async Task<Results<Created<MarketAccountResponse>, BadRequest<ValidationProblemDetails>>> (
                    WarehouseDbContext db,
                    CreateMarketAccountRequest marketAccountRequest,
                    ILoggerFactory loggerFactory,
                    int id) =>
                {
                    ValidationHelper.ValidateAndThrow(marketAccountRequest);
                    if (!await db.MarketDetails.AnyAsync(x => x.Id == id))
                    {
                        throw new ValidationException("Market not found");
                    }

                    if (await db.MarketAccounts.AnyAsync(x => x.MarketId == id))
                    {
                        throw new ValidationException("Account already exist for this market");
                    }

                    MarketAccount marketAccount = marketAccountRequest.AsEntity(id);

                    try
                    {
                        db.MarketAccounts.Add(marketAccount);
                        await db.SaveChangesAsync();
                        await db.Entry(marketAccount).Reference(x => x.MarketDetails).LoadAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAccountAPI.Create");
                        logger.LogError(ex, "Failed to create market account for MarketId: {MarketId}", id);
                        throw new ValidationException("Failed to create market account");
                    }

                    return TypedResults.Created($"/market/{id}/account", marketAccount.AsDto());
                })
            .WithName("CreateMarketCredential")
            .WithSummary("Create new market account")
            .Produces<MarketAccountResponse>(201)
            .Produces<string>(400);

        group.MapPut(
                "/{id:int}",
                async Task<Results<Ok, NotFound, BadRequest<ValidationProblemDetails>, BadRequest>> (
                    WarehouseDbContext db,
                    int id,
                    UpdateMarketAccountRequest marketAccountRequest,
                    ILoggerFactory loggerFactory) =>
                {
                    ValidationHelper.ValidateAndThrow(marketAccountRequest);
                    if (!await db.MarketDetails.AnyAsync(x => x.Id == id))
                    {
                        throw new ValidationException("Market not found");
                    }

                    if (!await db.MarketAccounts.AnyAsync(x => x.MarketId == id))
                    {
                        return TypedResults.NotFound();
                    }

                    try
                    {
                        int rowsAffected = await db.MarketAccounts.Where(x => x.MarketId == id)
                            .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.ApiKey, marketAccountRequest.ApiKey)
                                                    .SetProperty(x => x.Passphrase, marketAccountRequest.Passphrase)
                                                    .SetProperty(x => x.SecretKey, marketAccountRequest.SecretKey));

                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAccountAPI.Update");
                        logger.LogError(ex, "Failed to update market account for MarketId: {MarketId}", id);
                        return TypedResults.BadRequest();
                    }
                })
            .WithName("UpdateMarketAccount")
            .WithSummary("Update market account")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        group.MapDelete(
                "/{id:int}",
                async Task<Results<NotFound, Ok, BadRequest>> (WarehouseDbContext db, int id, ILoggerFactory loggerFactory) =>
                {
                    try
                    {
                        int rowsAffected = await db.MarketAccounts.Where(x => x.MarketId == id).ExecuteDeleteAsync();
                        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.Ok();
                    }
                    catch (DbUpdateException ex)
                    {
                        ILogger logger = loggerFactory.CreateLogger("MarketAccountAPI.Delete");
                        logger.LogError(ex, "Failed to delete market account for MarketId: {MarketId}", id);
                        return TypedResults.BadRequest();
                    }
                })
            .WithName("DeleteMarketCredential")
            .WithSummary("Delete market account")
            .Produces(200)
            .Produces(404)
            .Produces<string>(400);

        return group;
    }
}
