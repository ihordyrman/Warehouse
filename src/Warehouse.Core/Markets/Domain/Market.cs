using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Markets.Domain;

/// <summary>
///     Represents a trading exchange/market connection.
///     Markets can have associated credentials for API access.
/// </summary>
public class Market : AuditEntity
{
    /// <summary>
    ///     Unique identifier for this market connection.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     The type/name of the exchange (e.g., OKX, Binance).
    /// </summary>
    public MarketType Type { get; init; }

    /// <summary>
    ///     API credentials for authenticated access to this market.
    ///     Null if no credentials have been configured.
    /// </summary>
    public MarketCredentials? Credentials { get; init; }
}
