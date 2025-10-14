using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Markets.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Markets.Contracts;

public interface IBalanceManager
{
    Task<Result<Balance>> GetBalanceAsync(MarketType marketType, string currency, CancellationToken cancellationToken = default);

    Task<Result<BalanceSnapshot>> GetAllBalancesAsync(MarketType marketType, CancellationToken cancellationToken = default);

    Task<Result<AccountBalance>> GetAccountBalanceAsync(MarketType marketType, CancellationToken cancellationToken = default);

    Task<Result<List<Balance>>> GetNonZeroBalancesAsync(MarketType marketType, CancellationToken cancellationToken = default);
}
