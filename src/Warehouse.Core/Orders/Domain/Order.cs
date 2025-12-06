using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Orders.Domain;

/// <summary>
///     Represents a trade order placed on an exchange.
///     Tracks the full lifecycle from placement to execution/cancellation.
/// </summary>
public class Order : AuditEntity
{
    /// <summary>
    ///     Unique identifier for this order in the local database.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    ///     Foreign key to the Pipeline that created this order.
    ///     Null for manually placed orders.
    /// </summary>
    public int? PipelineId { get; set; }

    /// <summary>
    ///     The exchange this order was placed on.
    /// </summary>
    public MarketType MarketType { get; set; }

    /// <summary>
    ///     The order ID assigned by the exchange.
    ///     Used for querying order status and cancellation.
    /// </summary>
    public string ExchangeOrderId { get; set; } = string.Empty;

    /// <summary>
    ///     The trading pair symbol (e.g., "BTC-USDT").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this is a buy or sell order.
    /// </summary>
    public OrderSide Side { get; set; }

    /// <summary>
    ///     Current status of the order.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    ///     The quantity of the base asset to trade.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    ///     The limit price for the order.
    ///     Null for market orders.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    ///     The trigger price for stop orders.
    ///     When market reaches this price, the order is activated.
    /// </summary>
    public decimal? StopPrice { get; set; }

    /// <summary>
    ///     The trading fee charged by the exchange for this order.
    ///     Populated after execution.
    /// </summary>
    public decimal? Fee { get; set; }

    /// <summary>
    ///     When the order was submitted to the exchange.
    /// </summary>
    public DateTime? PlacedAt { get; set; }

    /// <summary>
    ///     When the order was fully executed (filled).
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    ///     When the order was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    ///     Take profit price level for attached TP/SL orders.
    ///     When reached, triggers a sell to lock in profits.
    /// </summary>
    public decimal? TakeProfit { get; set; }

    /// <summary>
    ///     Stop loss price level for attached TP/SL orders.
    ///     When reached, triggers a sell to limit losses.
    /// </summary>
    public decimal? StopLoss { get; set; }
}
