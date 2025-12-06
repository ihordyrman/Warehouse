namespace Warehouse.Core.Orders.Domain;

/// <summary>
///     The direction of a trade order.
/// </summary>
public enum OrderSide
{
    /// <summary>Buying the base asset (e.g., buying BTC in BTC-USDT).</summary>
    Buy,

    /// <summary>Selling the base asset (e.g., selling BTC in BTC-USDT).</summary>
    Sell
}
