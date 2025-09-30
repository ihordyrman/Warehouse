using Microsoft.EntityFrameworkCore;
using Warehouse.Backend.Core.Domain;
using Warehouse.Backend.Core.Infrastructure;

namespace Warehouse.Backend.Core.Application.Services;

public interface ICandlestickService
{
    Task<int> SaveCandlesticksAsync(IEnumerable<Candlestick> candlesticks, CancellationToken cancellationToken = default);

    Task<List<Candlestick>> GetCandlesticksAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        DateTime? from = null,
        DateTime? to = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    Task<Candlestick?> GetLatestCandlestickAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        CancellationToken cancellationToken = default);
}

public class CandlestickService(WarehouseDbContext dbContext, ILogger<CandlestickService> logger) : ICandlestickService
{
    public async Task<int> SaveCandlesticksAsync(IEnumerable<Candlestick> candlesticks, CancellationToken cancellationToken = default)
    {
        var candleList = candlesticks.ToList();
        int saved = 0;
        if (candleList.Count == 0)
        {
            return saved;
        }

        try
        {
            // todo: think about more optimal way
            foreach (Candlestick candlestick in candleList)
            {
                Candlestick? existing = await dbContext.Candlesticks.FirstOrDefaultAsync(
                    x => x.Symbol == candlestick.Symbol &&
                         x.MarketType == candlestick.MarketType &&
                         x.Timeframe == candlestick.Timeframe &&
                         x.Timestamp == candlestick.Timestamp,
                    cancellationToken);

                if (existing == null)
                {
                    dbContext.Candlesticks.Add(candlestick);
                    saved++;
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
                existing.Timestamp = candlestick.Timestamp;
                saved++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save {Count} candlesticks", candleList.Count);
            throw;
        }

        return saved;
    }

    public async Task<List<Candlestick>> GetCandlesticksAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        DateTime? from = null,
        DateTime? to = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Candlestick> query =
            dbContext.Candlesticks.Where(x => x.Symbol == symbol && x.MarketType == marketType && x.Timeframe == timeframe);

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

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Candlestick?> GetLatestCandlestickAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        CancellationToken cancellationToken = default)
        => await dbContext.Candlesticks.Where(x => x.Symbol == symbol && x.MarketType == marketType && x.Timeframe == timeframe)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
}
