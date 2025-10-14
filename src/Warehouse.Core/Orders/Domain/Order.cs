using Warehouse.Core.Markets.Domain;
using Warehouse.Core.Shared.Domain;

namespace Warehouse.Core.Orders.Domain;

public class Order : AuditEntity
{
    public long Id { get; set; }

    public int? WorkerId { get; set; }

    public MarketType MarketType { get; set; }

    public string ExchangeOrderId { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public OrderSide Side { get; set; }

    public OrderStatus Status { get; set; }

    public decimal Quantity { get; set; }

    public decimal? Price { get; set; }

    public decimal? StopPrice { get; set; }

    public decimal? Fee { get; set; }

    public DateTime? PlacedAt { get; set; }

    public DateTime? ExecutedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public decimal? TakeProfit { get; set; }

    public decimal? StopLoss { get; set; }
}
