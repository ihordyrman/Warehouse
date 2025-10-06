using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Contracts;

public interface ICredentialsProvider
{
    Task<MarketCredentials> GetCredentialsAsync(MarketType marketType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketCredentials>> GetAllCredentialsAsync(
        MarketType? marketType = null,
        CancellationToken cancellationToken = default);
}
