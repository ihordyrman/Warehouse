using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Backend.Endpoints.Models;
using Warehouse.Core.Application.Services;
using Warehouse.Core.Domain;

namespace Warehouse.Backend.Endpoints;

public static class CandlestickEndpoints
{
    public static RouteGroupBuilder MapCandlestickEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/candlesticks");
        group.WithTags("candlesticks");
        group.RequireRateLimiting("ApiPolicy");
        group.MapGet(
                "/{symbol}",
                async Task<Results<Ok<List<CandlestickResponse>>, BadRequest<string>>> (
                    ICandlestickService candlestickService,
                    string symbol,
                    [FromQuery] MarketType marketType = MarketType.Okx,
                    [FromQuery] string timeframe = "1m",
                    [FromQuery] DateTime? from = null,
                    [FromQuery] DateTime? to = null,
                    [FromQuery] int? limit = null) =>
                {
                    try
                    {
                        List<Candlestick> candlesticks = [];
                        await foreach (Candlestick candlestick in candlestickService.GetCandlesticksAsync(
                                           symbol,
                                           marketType,
                                           timeframe,
                                           from,
                                           to,
                                           limit))
                        {
                            candlesticks.Add(candlestick);
                        }

                        return TypedResults.Ok(candlesticks.Select(x => x.ToResponse()).ToList());
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.BadRequest($"Failed to get candlesticks: {ex.Message}");
                    }
                })
            .WithName("GetCandlesticks")
            .WithSummary("Get candlesticks for a symbol")
            .Produces<List<CandlestickResponse>>()
            .Produces<string>(400);

        group.MapGet(
                "/{symbol}/latest",
                async Task<Results<Ok<CandlestickResponse>, NotFound, BadRequest<string>>> (
                    ICandlestickService candlestickService,
                    string symbol,
                    [FromQuery] MarketType marketType = MarketType.Okx,
                    [FromQuery] string timeframe = "1m") =>
                {
                    try
                    {
                        Candlestick? candlestick = await candlestickService.GetLatestCandlestickAsync(symbol, marketType, timeframe);
                        return candlestick switch
                        {
                            null => TypedResults.NotFound(),
                            _ => TypedResults.Ok(candlestick.ToResponse())
                        };
                    }
                    catch (Exception ex)
                    {
                        return TypedResults.BadRequest($"Failed to get latest candlestick: {ex.Message}");
                    }
                })
            .WithName("GetLatestCandlestick")
            .WithSummary("Get latest candlestick for a symbol")
            .Produces<CandlestickResponse>()
            .Produces(404)
            .Produces<string>(400);

        return group;
    }
}
