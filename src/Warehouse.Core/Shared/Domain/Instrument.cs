// ReSharper disable InconsistentNaming

namespace Warehouse.Core.Shared.Domain;

/// <summary>
///     Supported cryptocurrency instruments/assets.
/// </summary>
public enum Instrument
{
    /// <summary>Tether USD stablecoin.</summary>
    USDT,

    /// <summary>Bitcoin.</summary>
    BTC,

    /// <summary>OKX native token.</summary>
    OKB,

    /// <summary>Solana.</summary>
    SOL,

    /// <summary>Ethereum.</summary>
    ETH,

    /// <summary>Dogecoin.</summary>
    DOGE,

    /// <summary>Ripple/XRP.</summary>
    XRP,

    /// <summary>Bitcoin Cash.</summary>
    BCH,

    /// <summary>Litecoin.</summary>
    LTC
}
