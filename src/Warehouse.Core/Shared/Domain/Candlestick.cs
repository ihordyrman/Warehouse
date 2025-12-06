using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Shared.Domain;

/// <summary>
///     Represents OHLCV (Open, High, Low, Close, Volume) candlestick data for a trading pair.
///     Used for chart visualization and technical analysis.
/// </summary>
public class Candlestick
{
    /// <summary>
    ///     Unique identifier for this candlestick.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    ///     The trading pair symbol (e.g., "BTC-USDT").
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    ///     The exchange this candlestick data is from.
    /// </summary>
    public MarketType MarketType { get; set; }

    /// <summary>
    ///     The start time of this candlestick period.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     The opening price at the start of the period.
    /// </summary>
    public decimal Open { get; set; }

    /// <summary>
    ///     The highest price reached during the period.
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    ///     The lowest price reached during the period.
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    ///     The closing price at the end of the period.
    /// </summary>
    public decimal Close { get; set; }

    /// <summary>
    ///     Trading volume in the base asset during the period.
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    ///     Trading volume in the quote asset during the period.
    /// </summary>
    public decimal VolumeQuote { get; set; }

    /// <summary>
    ///     Whether this candlestick period has ended.
    ///     False for the current/active candle which may still change.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    ///     The timeframe/interval for this candle (e.g., "1m", "5m", "1H", "1D").
    /// </summary>
    public required string Timeframe { get; set; }
}
