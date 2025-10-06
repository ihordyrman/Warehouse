using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;

namespace Warehouse.Backend.Endpoints.Models;

public abstract class CandlestickModel
{
    public long Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public MarketType MarketType { get; set; }

    public DateTime Timestamp { get; set; }

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public decimal Volume { get; set; }

    public decimal VolumeQuote { get; set; }

    public bool IsCompleted { get; set; }

    public string Timeframe { get; set; } = string.Empty;
}

public class CandlestickResponse : CandlestickModel;

public static class CandlestickMappingExtensions
{
    public static CandlestickResponse ToResponse(this Candlestick entity)
        => new()
        {
            Id = entity.Id,
            Symbol = entity.Symbol,
            MarketType = entity.MarketType,
            Timestamp = entity.Timestamp,
            Open = entity.Open,
            High = entity.High,
            Low = entity.Low,
            Close = entity.Close,
            Volume = entity.Volume,
            VolumeQuote = entity.VolumeQuote,
            IsCompleted = entity.IsCompleted,
            Timeframe = entity.Timeframe
        };
}
