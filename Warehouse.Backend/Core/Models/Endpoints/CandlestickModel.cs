using Warehouse.Backend.Core.Domain;

namespace Warehouse.Backend.Core.Models.Endpoints;

public class CandlestickModel
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
