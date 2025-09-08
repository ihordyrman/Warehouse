namespace Warehouse.Backend.Core.Models;

public sealed class MarketData(string instrument, string[][] asks, string[][] bids)
{
    public string Instrument { get; } = instrument;

    public string[][] Asks { get; init; } = asks;

    public string[][] Bids { get; init; } = bids;

    public decimal BidPrice { get; set; }

    public decimal AskPrice { get; set; }

    public bool Equals(MarketData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Instrument == other.Instrument;
    }

    public override int GetHashCode() => HashCode.Combine(Instrument);
}

public sealed class MarketDataCache(string instrument)
{
    public string Instrument { get; } = instrument;

    public OrderedDictionary<decimal, (decimal, int)> Asks { get; } = [];

    public OrderedDictionary<decimal, (decimal, int)> Bids { get; } = [];

    public bool Equals(MarketDataCache? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Instrument == other.Instrument;
    }

    public override int GetHashCode() => HashCode.Combine(Instrument);
}
