using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Warehouse.Core.Infrastructure.Persistence;
using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Orders.Contracts;
using Warehouse.Core.Orders.Domain;
using Warehouse.Core.Orders.Models;
using Warehouse.Core.Shared;

namespace Warehouse.Core.Orders.Services;

/// <summary>
///     Implementation of IOrderManager that uses EF Core for persistence and IMarketOrderProvider for execution.
/// </summary>
public class OrderManager : IOrderManager
{
    private readonly WarehouseDbContext dbContext;
    private readonly ILogger<OrderManager> logger;
    private readonly Dictionary<MarketType, IMarketOrderProvider> providers = [];

    public OrderManager(WarehouseDbContext dbContext, ILogger<OrderManager> logger, IServiceProvider serviceProvider)
    {
        this.dbContext = dbContext;
        this.logger = logger;

        IEnumerable<IMarketOrderProvider> marketProviders = serviceProvider.GetServices<IMarketOrderProvider>();

        foreach (IMarketOrderProvider provider in marketProviders)
        {
            providers[provider.MarketType] = provider;
            logger.LogInformation("Registered balance provider for {MarketType}", provider.MarketType);
        }
    }

    public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = new Order
            {
                PipelineId = request.PipelineId,
                MarketType = request.MarketType,
                Symbol = request.Symbol,
                Side = request.Side,
                Quantity = request.Quantity,
                Price = request.Price,
                StopPrice = request.StopPrice,
                TakeProfit = request.TakeProfit,
                StopLoss = request.StopLoss,
                Status = OrderStatus.Pending
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created order {OrderId} for {Symbol} {Side} {Quantity}",
                order.Id,
                order.Symbol,
                order.Side,
                order.Quantity);

            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order");
            return Result<Order>.Failure(new Error($"Failed to create order: {ex.Message}"));
        }
    }

    public async Task<Result<Order>> ExecuteOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            Order? order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
            if (order is null)
            {
                return Result<Order>.Failure(new Error($"Order {orderId} not found"));
            }

            if (order.Status != OrderStatus.Pending)
            {
                return Result<Order>.Failure(new Error($"Cannot place order in status {order.Status}"));
            }

            IMarketOrderProvider? orderProvider = providers.GetValueOrDefault(order.MarketType);
            if (orderProvider is null)
            {
                return Result<Order>.Failure(new Error($"No order provider registered for {order.MarketType}"));
            }

            Result<string> result = await orderProvider.ExecuteOrderAsync(order, cancellationToken);

            if (!result.IsSuccess)
            {
                order.Status = OrderStatus.Failed;
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result<Order>.Failure(result.Error);
            }

            order.ExchangeOrderId = result.Value;
            order.Status = OrderStatus.Placed;
            order.PlacedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Placed order {OrderId} on {Market} with exchange ID {ExchangeOrderId}",
                orderId,
                order.MarketType,
                order.ExchangeOrderId);

            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to place order {OrderId}", orderId);
            return Result<Order>.Failure(new Error($"Failed to place order: {ex.Message}"));
        }
    }

    public async Task<Result<Order>> UpdateOrderAsync(
        long orderId,
        UpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Order? order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
            if (order is null)
            {
                return Result<Order>.Failure(new Error($"Order {orderId} not found"));
            }

            if (order.Status is not OrderStatus.Placed and not OrderStatus.PartiallyFilled)
            {
                return Result<Order>.Failure(new Error($"Cannot update order in status {order.Status}"));
            }

            if (request.Quantity.HasValue)
            {
                order.Quantity = request.Quantity.Value;
            }

            if (request.Price.HasValue)
            {
                order.Price = request.Price.Value;
            }

            if (request.StopPrice.HasValue)
            {
                order.StopPrice = request.StopPrice.Value;
            }

            if (request.TakeProfit.HasValue)
            {
                order.TakeProfit = request.TakeProfit.Value;
            }

            if (request.StopLoss.HasValue)
            {
                order.StopLoss = request.StopLoss.Value;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated order {OrderId}", orderId);
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update order {OrderId}", orderId);
            return Result<Order>.Failure(new Error($"Failed to update order: {ex.Message}"));
        }
    }

    public async Task<Result<Order>> CancelOrderAsync(long orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Order? order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
            if (order is null)
            {
                return Result<Order>.Failure(new Error($"Order {orderId} not found"));
            }

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Filled)
            {
                return Result<Order>.Failure(new Error($"Cannot cancel order in status {order.Status}"));
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cancelled order {OrderId} with reason: {Reason}", orderId, reason);
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            return Result<Order>.Failure(new Error($"Failed to cancel order: {ex.Message}"));
        }
    }

    public async Task<Order?> GetOrderAsync(long orderId, CancellationToken cancellationToken = default)
        => await dbContext.Orders.FindAsync([orderId], cancellationToken);

    public async Task<List<Order>> GetOrdersAsync(int workerId, OrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = dbContext.Orders.Where(x => x.PipelineId == workerId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetOrderHistoryAsync(
        int skip = 0,
        int take = 100,
        OrderHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order>? query = dbContext.Orders.AsQueryable();

        if (filter is null)
        {
            return await query.OrderByDescending(x => x.CreatedAt).Skip(skip).Take(take).ToListAsync(cancellationToken);
        }

        if (filter.PipelineId.HasValue)
        {
            query = query.Where(x => x.PipelineId == filter.PipelineId.Value);
        }

        if (filter.MarketType.HasValue)
        {
            query = query.Where(x => x.MarketType == filter.MarketType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Symbol))
        {
            query = query.Where(x => x.Symbol == filter.Symbol);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.Side.HasValue)
        {
            query = query.Where(x => x.Side == filter.Side.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);
        }

        return await query.OrderByDescending(x => x.CreatedAt).Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetOrderByExchangeIdAsync(
        string exchangeOrderId,
        MarketType marketType,
        CancellationToken cancellationToken = default)
        => await dbContext.Orders.FirstOrDefaultAsync(
            x => x.ExchangeOrderId == exchangeOrderId && x.MarketType == marketType,
            cancellationToken);

    public async Task<decimal> GetTotalExposureAsync(MarketType? marketType = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = dbContext.Orders.Where(x => x.Status == OrderStatus.Placed ||
                                                              x.Status == OrderStatus.PartiallyFilled ||
                                                              x.Status == OrderStatus.Filled);

        if (marketType.HasValue)

        {
            query = query.Where(x => x.MarketType == marketType.Value);
        }

        List<Order> orders = await query.ToListAsync(cancellationToken);

        decimal totalExposure = 0;
        foreach (Order order in orders)
        {
            decimal quantity = order.Quantity;
            decimal price = order.Price ?? 0;
            totalExposure += quantity * price;
        }

        return totalExposure;
    }
}
