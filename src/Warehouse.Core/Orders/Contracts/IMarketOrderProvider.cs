using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Orders.Contracts;

public interface IMarketOrderProvider
{
    MarketType MarketType { get; }

    Task<Result<string>> ExecuteOrderAsync(Order order, CancellationToken cancellationToken = default);
}
