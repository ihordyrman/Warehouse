namespace Warehouse.Core.Shared.Domain;

/// <summary>
///     Represents a trading pair composed of a base and quote instrument.
///     For example, BTC-USDT means trading Bitcoin (base) against USDT (quote).
/// </summary>
/// <param name="Left">The base instrument (asset being bought/sold).</param>
/// <param name="Right">The quote instrument (asset used for pricing).</param>
public record Pair(Instrument Left, Instrument Right)
{
    /// <summary>
    ///     Returns the pair as a hyphen-separated string (e.g., "BTC-USDT").
    /// </summary>
    public override string ToString() => $"{Left.ToString()}-{Right.ToString()}";

    /// <summary>
    ///     Implicitly converts the pair to its string representation.
    /// </summary>
    public static implicit operator string(Pair p) => $"{p.Left.ToString()}-{p.Right.ToString()}";
}
