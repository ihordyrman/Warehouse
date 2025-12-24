using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Warehouse.Core.Old.Functional.Markets.Contracts;
using Warehouse.Core.Old.Functional.Markets.Domain;
using Warehouse.Core.Old.Functional.Shared.Domain;

namespace Warehouse.Core.Old.Shared.Services;

public class CandlestickService(IDbConnection db, ILogger<CandlestickService> logger) : ICandlestickService
{
    public async Task<int> SaveCandlesticksAsync(IEnumerable<Candlestick> candlesticks, CancellationToken cancellationToken = default)
    {
        var candleList = candlesticks.ToList();
        if (candleList.Count == 0)
        {
            return 0;
        }

        try
        {
            var groupedCandles = candleList.GroupBy(x => new { x.Symbol, x.MarketType, x.Timeframe }).ToList();

            var totalSaved = 0;

            foreach (var group in groupedCandles)
            {
                var timestamps = group.Select(c => c.Timestamp).Distinct().ToList();

                // Dictionary<DateTime, Candlestick> existingCandles = await dbContext.Candlesticks
                //     .Where(x => x.Symbol == group.Key.Symbol &&
                //                 x.MarketType == group.Key.MarketType &&
                //                 x.Timeframe == group.Key.Timeframe &&
                //                 timestamps.Contains(x.Timestamp))
                //     .ToDictionaryAsync(x => x.Timestamp, x => x, cancellationToken);
                var existingCandles = new Dictionary<DateTime, Candlestick>();

                var toAdd = new List<Candlestick>();
                var toUpdate = new List<Candlestick>();

                foreach (Candlestick candlestick in group)
                {
                    if (!existingCandles.TryGetValue(candlestick.Timestamp, out Candlestick? existing))
                    {
                        toAdd.Add(candlestick);
                        continue;
                    }

                    if (existing.IsCompleted && candlestick.IsCompleted)
                    {
                        continue;
                    }

                    existing.Open = candlestick.Open;
                    existing.High = candlestick.High;
                    existing.Low = candlestick.Low;
                    existing.Close = candlestick.Close;
                    existing.Volume = candlestick.Volume;
                    existing.VolumeQuote = candlestick.VolumeQuote;
                    existing.IsCompleted = candlestick.IsCompleted;
                    toUpdate.Add(existing);
                }

                if (toAdd.Count > 0)
                {
                    // dbContext.Candlesticks.AddRange(toAdd);
                    totalSaved += toAdd.Count;
                }

                totalSaved += toUpdate.Count;
            }

            // await dbContext.SaveChangesAsync(cancellationToken);
            return totalSaved;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save {Count} candlesticks", candleList.Count);
            throw;
        }
    }

    public async IAsyncEnumerable<Candlestick> GetCandlesticksAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        DateTime? from = null,
        DateTime? to = null,
        int? limit = null)
    {
        // IQueryable<Candlestick> query =
        //     dbContext.Candlesticks.Where(x => x.Symbol == symbol && x.MarketType == (int)marketType && x.Timeframe == timeframe);

        var query = new List<Candlestick>().AsQueryable()
            .Where(x => x.Symbol == symbol && x.MarketType == (int)marketType && x.Timeframe == timeframe);

        if (from.HasValue)
        {
            query = query.Where(x => x.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Timestamp <= to.Value);
        }

        query = query.OrderByDescending(x => x.Timestamp);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        // await foreach (Candlestick candlestick in query.AsAsyncEnumerable())
        // {
        //     yield return candlestick;
        // }
        foreach (Candlestick candlestick in query)
        {
            yield return candlestick;
        }
    }

    public async Task<FSharpOption<Candlestick>> GetLatestCandlestickAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        // Candlestick? result = await dbContext.Candlesticks
        //     .Where(x => x.Symbol == symbol && x.MarketType == (int)marketType && x.Timeframe == timeframe)
        //     .OrderByDescending(x => x.Timestamp)
        //     .FirstOrDefaultAsync(cancellationToken);
        // return result is null ? FSharpOption<Candlestick>.None : FSharpOption<Candlestick>.Some(result);

        return null;
    }
}
