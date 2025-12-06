namespace Warehouse.Core.Markets.Domain;

/// <summary>
///     State of a WebSocket connection to an exchange.
/// </summary>
public enum ConnectionState
{
    /// <summary>Not connected to the exchange.</summary>
    Disconnected,

    /// <summary>Connection attempt in progress.</summary>
    Connecting,

    /// <summary>Successfully connected and ready for trading.</summary>
    Connected,

    /// <summary>Connection failed due to an error.</summary>
    Failed
}
