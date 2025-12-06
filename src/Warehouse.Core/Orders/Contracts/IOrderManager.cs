using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Orders.Contracts;

/// <summary>
///     Central service for managing the lifecycle of orders.
///     Handles creation, execution (placement on exchange), updates, and cancellation.
/// </summary>
public interface IOrderManager
{
    /// <summary>
    ///     Creates a new local order record.
    ///     Does not submit to exchange immediately (status will be Pending).
    /// </summary>
    Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Submits a pending order to the exchange.
    ///     Updates status to Placed or Failed based on the outcome.
    /// </summary>
    Task<Result<Order>> ExecuteOrderAsync(long orderId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Modifies an existing open order (price, quantity, etc.).
    /// </summary>
    Task<Result<Order>> UpdateOrderAsync(long orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cancels an open order on the exchange.
    /// </summary>
    Task<Result<Order>> CancelOrderAsync(long orderId, string? reason = null, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderAsync(long orderId, CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrdersAsync(int workerId, OrderStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves historical orders based on filter criteria.
    /// </summary>
    Task<List<Order>> GetOrderHistoryAsync(
        int skip = 0,
        int take = 100,
        OrderHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);
}
