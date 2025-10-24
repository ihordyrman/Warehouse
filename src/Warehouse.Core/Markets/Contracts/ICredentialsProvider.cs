using Warehouse.Core.Markets.Domain;

namespace Warehouse.Core.Markets.Contracts;

public interface ICredentialsProvider
{
    Task<MarketAccount> GetCredentialsAsync(MarketType marketType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketAccount>> GetAllCredentialsAsync(
        MarketType? marketType = null,
        CancellationToken cancellationToken = default);
}
