namespace Warehouse.Backend.Okx.Models;

public sealed class MarketData(string channel, string instrument, string[][] asks, string[][] bids)
{
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

    public string Channel { get; } = channel;

    public string Instrument { get; } = instrument;

    public string[][] Asks { get; init; } = asks;

    public string[][] Bids { get; init; } = bids;
}
