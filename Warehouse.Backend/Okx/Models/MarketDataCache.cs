namespace Warehouse.Backend.Okx.Models;

public sealed class MarketDataCache(string channel, string instrument)
{
    public MarketDataKey Key => new(Channel, Instrument);

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

    public string Channel { get; } = channel;

    public string Instrument { get; } = instrument;

    public OrderedDictionary<decimal, (decimal, int)> Asks { get; } = [];

    public OrderedDictionary<decimal, (decimal, int)> Bids { get; } = [];
}

public sealed record MarketDataKey(string Channel, string Instrument);
