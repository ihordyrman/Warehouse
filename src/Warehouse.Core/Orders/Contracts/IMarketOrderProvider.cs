using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Orders.Contracts;

/// <summary>
///     Interface for executing orders on specific markets.
///     Implementations handle market-specific API calls.
/// </summary>
public interface IMarketOrderProvider
{
    MarketType MarketType { get; }

    Task<Result<string>> ExecuteOrderAsync(Order order, CancellationToken cancellationToken = default);
}
