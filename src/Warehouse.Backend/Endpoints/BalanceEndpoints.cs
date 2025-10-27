using Microsoft.AspNetCore.Http.HttpResults;
using Warehouse.Backend.Endpoints.Models;
using Warehouse.Core.Markets.Contracts;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Backend.Endpoints;

public static class BalanceEndpoints
{
    public static RouteGroupBuilder MapBalanceEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/balance");
        group.WithTags("balance");
        group.RequireRateLimiting("ApiPolicy");

        group.MapGet(
                "/{marketType}",
                async Task<Results<Ok<BalanceSnapshotResponse>, BadRequest<string>>> (IBalanceManager balanceManager, string marketType) =>
                {
                    if (!Enum.TryParse(marketType, true, out MarketType market))
                    {
                        return TypedResults.BadRequest($"Invalid market type: {marketType}");
                    }

                    Result<BalanceSnapshot> result = await balanceManager.GetAllBalancesAsync(market);

                    if (!result.IsSuccess)
                    {
                        return TypedResults.BadRequest(result.Error.Message);
                    }

                    return TypedResults.Ok(BalanceSnapshotResponse.FromDomain(result.Value));
                })
            .WithName("GetMarketBalances")
            .WithSummary("Get all balances for a specific market")
            .Produces<BalanceSnapshotResponse>()
            .Produces<string>(400);

        group.MapGet(
                "/{marketType}/{currency}",
                async Task<Results<Ok<BalanceResponse>, NotFound, BadRequest<string>>> (
                    IBalanceManager balanceManager,
                    string marketType,
                    string currency) =>
                {
                    if (!Enum.TryParse(marketType, true, out MarketType market))
                    {
                        return TypedResults.BadRequest($"Invalid market type: {marketType}");
                    }

                    Result<Balance> result = await balanceManager.GetBalanceAsync(market, currency.ToUpperInvariant());

                    if (!result.IsSuccess)
                    {
                        return result.Error.Message.Contains("not found") ?
                            TypedResults.NotFound() :
                            TypedResults.BadRequest(result.Error.Message);
                    }

                    return TypedResults.Ok(BalanceResponse.FromDomain(result.Value));
                })
            .WithName("GetCurrencyBalance")
            .WithSummary("Get balance for a specific currency on a specific market")
            .Produces<BalanceResponse>()
            .Produces(404)
            .Produces<string>(400);

        group.MapGet(
                "/{marketType}/account/summary",
                async Task<Results<Ok<AccountBalanceResponse>, BadRequest<string>>> (IBalanceManager balanceManager, string marketType) =>
                {
                    if (!Enum.TryParse(marketType, true, out MarketType market))
                    {
                        return TypedResults.BadRequest($"Invalid market type: {marketType}");
                    }

                    Result<AccountBalance> result = await balanceManager.GetAccountBalanceAsync(market);

                    if (!result.IsSuccess)
                    {
                        return TypedResults.BadRequest(result.Error.Message);
                    }

                    return TypedResults.Ok(AccountBalanceResponse.FromDomain(result.Value));
                })
            .WithName("GetAccountSummary")
            .WithSummary("Get account summary for a specific market")
            .Produces<AccountBalanceResponse>()
            .Produces<string>(400);

        group.MapGet(
                "/{marketType}/total-usdt",
                async Task<Results<Ok<TotalValueResponse>, BadRequest<string>>> (IBalanceManager balanceManager, string marketType) =>
                {
                    if (!Enum.TryParse(marketType, true, out MarketType market))
                    {
                        return TypedResults.BadRequest($"Invalid market type: {marketType}");
                    }

                    Result<decimal> result = await balanceManager.GetTotalUsdtValueAsync(market);

                    if (!result.IsSuccess)
                    {
                        return TypedResults.BadRequest(result.Error.Message);
                    }

                    return TypedResults.Ok(new TotalValueResponse { TotalUsdtValue = result.Value });
                })
            .WithName("GetTotalUsdtValue")
            .WithSummary("Get total value of all balances in USDT equivalent")
            .Produces<TotalValueResponse>()
            .Produces<string>(400);

        return group;
    }
}
