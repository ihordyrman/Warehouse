namespace Warehouse.Backend.Core.Models;

public sealed class MarketData(string channel, string instrument, string[][] asks, string[][] bids)
{
    public string Channel { get; } = channel;

    public string Instrument { get; } = instrument;

    public string[][] Asks { get; init; } = asks;

    public string[][] Bids { get; init; } = bids;

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

        return Channel == other.Channel && Instrument == other.Instrument;
    }

    public override int GetHashCode() => HashCode.Combine(Channel, Instrument);
}

public sealed class MarketDataCache(string channel, string instrument)
{
    public MarketDataKey Key => new(Channel, Instrument);

    public string Channel { get; } = channel;

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

        return Channel == other.Channel && Instrument == other.Instrument;
    }

    public override int GetHashCode() => HashCode.Combine(Channel, Instrument);
}

public sealed record MarketDataKey(string Channel, string Instrument);
