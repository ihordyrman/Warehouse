using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Orders.Contracts;

public interface IOrderManager
{
    Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<Result<Order>> ExecuteOrderAsync(long orderId, CancellationToken cancellationToken = default);

    Task<Result<Order>> UpdateOrderAsync(long orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default);

    Task<Result<Order>> CancelOrderAsync(long orderId, string? reason = null, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderAsync(long orderId, CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrdersByWorkerAsync(int workerId, OrderStatus? status = null, CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrderHistoryAsync(
        int skip = 0,
        int take = 100,
        OrderHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);
}
