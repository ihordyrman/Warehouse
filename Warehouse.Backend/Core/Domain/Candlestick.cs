namespace Warehouse.Backend.Core.Domain;

public class Candlestick
{
    public long Id { get; set; }

    public required string Symbol { get; set; }

    public MarketType MarketType { get; set; }

    public DateTime Timestamp { get; set; }

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public decimal Volume { get; set; }

    public decimal VolumeQuote { get; set; }

    public bool IsCompleted { get; set; }

    public required string Timeframe { get; set; }
}
