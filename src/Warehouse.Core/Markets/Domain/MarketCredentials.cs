using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Markets.Domain;

/// <summary>
///     Stores API credentials for authenticating with a trading exchange.
///     Contains sensitive data that should be encrypted at rest.
/// </summary>
public class MarketCredentials : AuditEntity
{
    /// <summary>
    ///     Unique identifier for these credentials.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     Foreign key to the parent Market.
    /// </summary>
    public int MarketId { get; init; }

    /// <summary>
    ///     Navigation property to the owning Market.
    /// </summary>
    public Market Market { get; init; } = null!;

    /// <summary>
    ///     The API key provided by the exchange.
    ///     Used for identifying the account in API requests.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    ///     The passphrase for API authentication.
    ///     Required by some exchanges (e.g., OKX) as an additional security layer.
    /// </summary>
    public string Passphrase { get; set; } = string.Empty;

    /// <summary>
    ///     The secret key for signing API requests.
    ///     Should never be exposed or logged.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///     Whether these credentials connect to the exchange's sandbox/test environment.
    ///     Use sandbox mode for testing without real funds.
    /// </summary>
    public bool IsSandbox { get; set; } = false;
}
