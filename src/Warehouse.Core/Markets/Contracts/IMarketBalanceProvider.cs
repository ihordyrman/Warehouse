using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Markets.Contracts;

public interface IMarketBalanceProvider
{
    MarketType MarketType { get; }

    Task<Result<BalanceSnapshot>> GetBalancesAsync(CancellationToken cancellationToken = default);

    Task<Result<Balance>> GetBalanceAsync(string currency, CancellationToken cancellationToken = default);

    Task<Result<decimal>> GetTotalUsdtValueAsync(CancellationToken cancellationToken = default);
}
