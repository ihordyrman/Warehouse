using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Contracts;

/// <summary>
///     Provides secure access to market API credentials.
/// </summary>
public interface ICredentialsProvider
{
    Task<MarketCredentials> GetCredentialsAsync(MarketType marketType, CancellationToken cancellationToken = default);
}
